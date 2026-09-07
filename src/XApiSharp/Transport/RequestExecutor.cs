using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Errors;

namespace XApiSharp.Transport;

/// <summary>
/// The single shared request path every typed endpoint method goes through - auth, sending,
/// error mapping, and deserialization live here once, not duplicated per endpoint (spec
/// section 6.1: "Нельзя генерировать отдельную независимую реализацию retry или авторизации для
/// каждого endpoint"). Retry/backoff/rate-limit waiting land in a later E3 commit; this covers
/// the transport-timeout baseline (HTTP-01..12).
/// </summary>
internal sealed class RequestExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IXAuthenticationProvider _authenticationProvider;
    private readonly XClientOptions _options;
    private readonly TimeProvider _timeProvider;

    public RequestExecutor(HttpClient httpClient, IXAuthenticationProvider authenticationProvider, XClientOptions options, TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _authenticationProvider = authenticationProvider;
        _options = options;
        _timeProvider = timeProvider;
    }

    public async Task<XResponse<TBody>> SendAsync<TBody>(HttpMethod method, string relativePath, CancellationToken cancellationToken)
    {
        // HTTP-06: cancellation must propagate deterministically end to end - do not rely on the
        // HttpClient/handler pipeline to observe an already-cancelled token on its own.
        cancellationToken.ThrowIfCancellationRequested();

        // HTTP-07: the operation deadline spans the whole call (auth + every attempt + body
        // read); the attempt deadline below covers only a single HTTP send/headers phase. Both
        // are driven by the injected TimeProvider so tests never need a real sleep.
        using var operationTimeoutCts = new CancellationTokenSource(_options.OperationTimeout, _timeProvider);
        using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, operationTimeoutCts.Token);

        using var request = new HttpRequestMessage(method, new Uri(_options.BaseUrl, relativePath));
        await _authenticationProvider.PrepareRequestAsync(request, operationCts.Token).ConfigureAwait(false);

        HttpResponseMessage response;
        using (var attemptTimeoutCts = new CancellationTokenSource(_options.AttemptTimeout, _timeProvider))
        using (var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(operationCts.Token, attemptTimeoutCts.Token))
        {
            try
            {
                response = await _httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, attemptCts.Token)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new XTransportException("Request failed at the transport level.", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw ClassifyCancellation(ex, new CancellationContext(cancellationToken, operationCts.Token, attemptCts.Token));
            }
        }

        using (response)
        {
            var headers = ToHeaderDictionary(response);
            var rateLimit = XRateLimitInfo.FromHeaders(response.Headers);

            if (!response.IsSuccessStatusCode)
            {
                throw await BuildExceptionAsync(response, cancellationToken, operationCts.Token).ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return new XResponse<TBody>
                {
                    Body = default,
                    StatusCode = response.StatusCode,
                    Headers = headers,
                    RateLimit = rateLimit,
                };
            }

            TBody? body;
            try
            {
                var stream = await response.Content.ReadAsStreamAsync(operationCts.Token).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    body = await JsonSerializer.DeserializeAsync<TBody>(stream, JsonOptions, operationCts.Token).ConfigureAwait(false);
                }
            }
            catch (JsonException ex)
            {
                throw new XProtocolException(
                    "Response body was not valid JSON for the expected contract.",
                    response.StatusCode,
                    innerException: ex);
            }
            catch (OperationCanceledException ex)
            {
                throw ClassifyCancellation(ex, new CancellationContext(cancellationToken, operationCts.Token, Attempt: null));
            }

            return new XResponse<TBody>
            {
                Body = body,
                StatusCode = response.StatusCode,
                Headers = headers,
                RateLimit = rateLimit,
            };
        }
    }

    // The three tokens represent different scopes (caller/operation/attempt), not a single
    // "the" cancellation token for the method - CA1068's last-parameter convention doesn't apply.
#pragma warning disable CA1068
    private readonly record struct CancellationContext(CancellationToken Caller, CancellationToken Operation, CancellationToken? Attempt);
#pragma warning restore CA1068

    /// <summary>
    /// Distinguishes four causes behind an <see cref="OperationCanceledException"/> so the caller
    /// gets an unambiguous result: the caller's own token, our operation/attempt deadlines, or the
    /// external <see cref="HttpClient.Timeout"/> (spec HTTP-08) - never a generic cancellation
    /// that leaves the caller guessing which one fired.
    /// </summary>
    private static Exception ClassifyCancellation(OperationCanceledException ex, CancellationContext context)
    {
        if (ex is TaskCanceledException { InnerException: TimeoutException })
        {
            return new XRequestTimeoutException(
                "The request timed out because the external HttpClient.Timeout is shorter than XClientOptions.OperationTimeout/AttemptTimeout. See the XClientOptions.AttemptTimeout XML doc remarks.",
                ex);
        }

        if (context.Caller.IsCancellationRequested)
        {
            return ex;
        }

        if (context.Operation.IsCancellationRequested)
        {
            return new XRequestTimeoutException("The request exceeded XClientOptions.OperationTimeout.", ex);
        }

        if (context.Attempt is { IsCancellationRequested: true })
        {
            return new XRequestTimeoutException("The request exceeded XClientOptions.AttemptTimeout.", ex);
        }

        return ex;
    }

    private static async Task<XApiException> BuildExceptionAsync(HttpResponseMessage response, CancellationToken callerToken, CancellationToken operationToken)
    {
        XProblem? problem = null;
        try
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is "application/problem+json" or "application/json")
            {
                var stream = await response.Content.ReadAsStreamAsync(operationToken).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    problem = await JsonSerializer.DeserializeAsync<XProblem>(stream, JsonOptions, operationToken).ConfigureAwait(false);
                }
            }
        }
        catch (JsonException)
        {
            // Best-effort only - fall through with problem == null rather than hide the
            // original HTTP status behind a secondary parsing failure.
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            // Best-effort only here too - the HTTP status itself is the primary signal; a
            // deadline hit while reading the error body shouldn't hide it behind a timeout.
        }

        var message = problem?.Title ?? $"X API request failed with status {(int)response.StatusCode}.";

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new XAuthenticationException(message, response.StatusCode, problem),
            HttpStatusCode.Forbidden => new XAccessDeniedException(message, response.StatusCode, problem),
            HttpStatusCode.TooManyRequests => new XRateLimitException(message, response.StatusCode, problem)
            {
                RetryAfter = response.Headers.RetryAfter?.Delta,
            },
            _ => new XApiException(message, response.StatusCode, problem),
        };
    }

    private static Dictionary<string, IReadOnlyList<string>> ToHeaderDictionary(HttpResponseMessage response)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            result[header.Key] = header.Value.ToList();
        }

        foreach (var header in response.Content.Headers)
        {
            result[header.Key] = header.Value.ToList();
        }

        return result;
    }
}

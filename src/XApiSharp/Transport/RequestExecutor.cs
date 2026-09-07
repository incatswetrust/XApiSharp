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
/// каждого endpoint"). Retry/backoff/rate-limit waiting are added in E3; this is the E2 baseline.
/// </summary>
internal sealed class RequestExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IXAuthenticationProvider _authenticationProvider;
    private readonly XClientOptions _options;

    public RequestExecutor(HttpClient httpClient, IXAuthenticationProvider authenticationProvider, XClientOptions options)
    {
        _httpClient = httpClient;
        _authenticationProvider = authenticationProvider;
        _options = options;
    }

    public async Task<XResponse<TBody>> SendAsync<TBody>(HttpMethod method, string relativePath, CancellationToken cancellationToken)
    {
        // HTTP-06: cancellation must propagate deterministically end to end - do not rely on the
        // HttpClient/handler pipeline to observe an already-cancelled token on its own.
        cancellationToken.ThrowIfCancellationRequested();

        using var request = new HttpRequestMessage(method, new Uri(_options.BaseUrl, relativePath));
        await _authenticationProvider.PrepareRequestAsync(request, cancellationToken).ConfigureAwait(false);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new XTransportException("Request failed at the transport level.", ex);
        }

        using (response)
        {
            var headers = ToHeaderDictionary(response);
            var rateLimit = XRateLimitInfo.FromHeaders(response.Headers);

            if (!response.IsSuccessStatusCode)
            {
                throw await BuildExceptionAsync(response, cancellationToken).ConfigureAwait(false);
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
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                body = await JsonSerializer.DeserializeAsync<TBody>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                throw new XProtocolException(
                    "Response body was not valid JSON for the expected contract.",
                    response.StatusCode,
                    innerException: ex);
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

    private static async Task<XApiException> BuildExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        XProblem? problem = null;
        try
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is "application/problem+json" or "application/json")
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                problem = await JsonSerializer.DeserializeAsync<XProblem>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (JsonException)
        {
            // Best-effort only - fall through with problem == null rather than hide the
            // original HTTP status behind a secondary parsing failure.
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

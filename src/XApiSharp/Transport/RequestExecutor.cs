using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Errors;

namespace XApiSharp.Transport;

/// <summary>
/// The single shared request path every typed endpoint method goes through - auth, sending,
/// error mapping, retry, and deserialization live here once, not duplicated per endpoint (spec
/// section 6.1: "Нельзя генерировать отдельную независимую реализацию retry или авторизации для
/// каждого endpoint"). Rate-limit *state persistence* across calls (RATE-03..05) lands with the
/// per-context work in a later E3 commit; this covers per-call retry/backoff (spec section 13.2)
/// and header-driven 429 waiting (RATE-01/02/06).
/// </summary>
internal sealed class RequestExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HttpMethod[] RetryableMethods = [HttpMethod.Get, HttpMethod.Head];

    private readonly HttpClient _httpClient;
    private readonly IXAuthenticationProvider _authenticationProvider;
    private readonly XClientOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Random _jitterSource;

    public RequestExecutor(HttpClient httpClient, IXAuthenticationProvider authenticationProvider, XClientOptions options, TimeProvider timeProvider, Random jitterSource)
    {
        _httpClient = httpClient;
        _authenticationProvider = authenticationProvider;
        _options = options;
        _timeProvider = timeProvider;
        _jitterSource = jitterSource;
    }

    public Task<XResponse<TBody>> SendAsync<TBody>(HttpMethod method, string relativePath, CancellationToken cancellationToken) =>
        SendAsync<TBody>(method, relativePath, jsonBody: null, queryParameters: null, cancellationToken);

    /// <param name="method">HTTP method.</param>
    /// <param name="relativePath">Path relative to <see cref="XClientOptions.BaseUrl"/>.</param>
    /// <param name="queryParameters">Optional query parameters, built via
    /// <see cref="QueryStringBuilder"/> (SER-10) - entries with a null/empty value are omitted,
    /// never sent as an empty query string.</param>
    /// <param name="cancellationToken">Caller cancellation, layered under the operation/attempt
    /// deadlines (spec HTTP-06/07).</param>
    public Task<XResponse<TBody>> SendAsync<TBody>(HttpMethod method, string relativePath, IReadOnlyList<(string Name, string? Value)>? queryParameters, CancellationToken cancellationToken) =>
        SendAsync<TBody>(method, relativePath, jsonBody: null, queryParameters, cancellationToken);

    /// <param name="method">HTTP method.</param>
    /// <param name="relativePath">Path relative to <see cref="XClientOptions.BaseUrl"/>.</param>
    /// <param name="jsonBody">Serialized as the request's JSON body when non-null
    /// (<c>application/json</c>). Re-serialized fresh on every retry attempt, same as the rest of
    /// the request (HTTP-03) - a request body isn't a stream that gets consumed once.</param>
    /// <param name="queryParameters">Optional query parameters, built via
    /// <see cref="QueryStringBuilder"/> (SER-10) - entries with a null/empty value are omitted,
    /// never sent as an empty query string.</param>
    /// <param name="cancellationToken">Caller cancellation, layered under the operation/attempt
    /// deadlines (spec HTTP-06/07).</param>
    public Task<XResponse<TBody>> SendAsync<TBody>(HttpMethod method, string relativePath, object? jsonBody, IReadOnlyList<(string Name, string? Value)>? queryParameters, CancellationToken cancellationToken) =>
        ExecuteAsync(method, relativePath, jsonBody, binaryBody: null, contentFactory: null, queryParameters, ReadJsonBodyAsync<TBody>, cancellationToken);

    /// <summary>
    /// Same auth/retry/error-mapping path as <see cref="SendAsync{TBody}(HttpMethod, string, object?, IReadOnlyList{ValueTuple{string, string?}}?, CancellationToken)"/>,
    /// but for a binary response (e.g. DM media download) instead of JSON (spec section 9: "поддерживаются
    /// пустые, бинарные и специфические ответы"). Buffered, not streamed back to the caller -
    /// bounded by the same <see cref="XClientOptions.MaxResponseBufferSize"/> as every other
    /// response; genuinely large media transfer is the chunked-upload family's concern (E5), not
    /// this simple download.
    /// </summary>
    public Task<XResponse<byte[]>> SendForBytesAsync(HttpMethod method, string relativePath, CancellationToken cancellationToken) =>
        ExecuteAsync<byte[]>(method, relativePath, jsonBody: null, binaryBody: null, contentFactory: null, queryParameters: null, ReadBytesBodyAsync, cancellationToken);

    /// <param name="method">HTTP method.</param>
    /// <param name="relativePath">Path relative to <see cref="XClientOptions.BaseUrl"/>.</param>
    /// <param name="queryParameters">Optional query parameters, built via
    /// <see cref="QueryStringBuilder"/> (SER-10).</param>
    /// <param name="cancellationToken">Caller cancellation, layered under the operation/attempt
    /// deadlines (spec HTTP-06/07).</param>
    public Task<XResponse<byte[]>> SendForBytesAsync(HttpMethod method, string relativePath, IReadOnlyList<(string Name, string? Value)>? queryParameters, CancellationToken cancellationToken) =>
        ExecuteAsync<byte[]>(method, relativePath, jsonBody: null, binaryBody: null, contentFactory: null, queryParameters, ReadBytesBodyAsync, cancellationToken);

    /// <summary>
    /// Same as <see cref="SendAsync{TBody}(HttpMethod, string, object?, IReadOnlyList{ValueTuple{string, string?}}?, CancellationToken)"/>
    /// but for an <c>application/octet-stream</c> request body (e.g. compliance job submission
    /// upload) instead of JSON - the request-side counterpart to the binary-response overloads
    /// above.
    /// </summary>
    public Task<XResponse<TBody>> SendBytesAsync<TBody>(HttpMethod method, string relativePath, byte[] binaryBody, IReadOnlyList<(string Name, string? Value)>? queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binaryBody);

        return ExecuteAsync(method, relativePath, jsonBody: null, binaryBody, contentFactory: null, queryParameters, ReadJsonBodyAsync<TBody>, cancellationToken);
    }

    /// <summary>
    /// Same core path again, but for a caller-built <see cref="HttpContent"/> (media upload's
    /// <c>multipart/form-data</c> bodies) - <paramref name="contentFactory"/> is invoked fresh on
    /// every attempt (HTTP-03), same reasoning as the JSON/binary body overloads elsewhere:
    /// content that already made one attempt can't be resent as-is. Callers that build
    /// content around a non-seekable <see cref="Stream"/> should only pass a factory here when
    /// they can either tolerate a single retry consuming the stream (i.e. accept no retry after
    /// the first byte is sent) or the factory captures already-buffered bytes (e.g. one bounded
    /// chunked-upload segment) rather than the live stream itself.
    /// </summary>
    public Task<XResponse<TBody>> SendMultipartAsync<TBody>(HttpMethod method, string relativePath, Func<HttpContent> contentFactory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentFactory);

        return ExecuteAsync<TBody>(method, relativePath, jsonBody: null, binaryBody: null, contentFactory, queryParameters: null, ReadJsonBodyAsync<TBody>, cancellationToken);
    }

    /// <summary>
    /// The shared attempt loop (auth prep, transient-error/429/401-refresh retry, error mapping)
    /// every <c>Send*Async</c> overload goes through - only what happens with a *successful*
    /// response varies, via <paramref name="readBody"/>.
    /// </summary>
    private async Task<XResponse<TBody>> ExecuteAsync<TBody>(
        HttpMethod method,
        string relativePath,
        object? jsonBody,
        byte[]? binaryBody,
        Func<HttpContent>? contentFactory,
        IReadOnlyList<(string Name, string? Value)>? queryParameters,
        Func<HttpResponseMessage, IReadOnlyDictionary<string, IReadOnlyList<string>>, XRateLimitInfo?, CancellationToken, CancellationToken, Task<XResponse<TBody>>> readBody,
        CancellationToken cancellationToken)
    {
        // HTTP-06: cancellation must propagate deterministically end to end - do not rely on the
        // HttpClient/handler pipeline to observe an already-cancelled token on its own.
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = relativePath + (queryParameters is null ? null : QueryStringBuilder.Build(queryParameters));

        // HTTP-07: the operation deadline spans the whole call - every attempt, every retry
        // wait, and the body read; the attempt deadline covers a single HTTP send/headers
        // phase. Both are driven by the injected TimeProvider so tests never need a real sleep.
        using var operationTimeoutCts = new CancellationTokenSource(_options.OperationTimeout, _timeProvider);
        using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, operationTimeoutCts.Token);

        var isRetryableMethod = Array.IndexOf(RetryableMethods, method) >= 0;
        var maxAttempts = 1 + _options.MaxRetries;
        var hasRefreshedForThisCall = false;

        for (var attempt = 1; ; attempt++)
        {
            var isLastAttempt = attempt >= maxAttempts;

            // HTTP-03: a fresh HttpRequestMessage (and re-run auth prep) on every attempt - a
            // consumed request/refreshed token can't be resent as-is.
            using var request = new HttpRequestMessage(method, new Uri(_options.BaseUrl, fullPath));
            if (jsonBody is not null)
            {
                request.Content = JsonContent.Create(jsonBody, jsonBody.GetType(), options: JsonOptions);
            }
            else if (binaryBody is not null)
            {
                request.Content = new ByteArrayContent(binaryBody);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            }
            else if (contentFactory is not null)
            {
                request.Content = contentFactory();
            }

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
                    // Retry table (spec 13.2): "GET/HEAD с временной сетевой ошибкой ... |
                    // Ограниченный exponential backoff с jitter". Writes are never retried here.
                    if (isRetryableMethod && !isLastAttempt)
                    {
                        await DelayAsync(ComputeBackoffDelay(attempt), operationCts.Token, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

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
                    // Retry table (spec 13.2): "Истёкший OAuth 2.0 access token | Не более
                    // одного refresh; последующая отправка только при однозначно допустимом
                    // сценарии" - a 401 means the request was rejected before any side effect
                    // ran, so retrying once after a refresh is safe even for a write method
                    // (unlike the transient-5xx/429 retries below, which stay GET/HEAD-only).
                    if (response.StatusCode == HttpStatusCode.Unauthorized
                        && !hasRefreshedForThisCall
                        && _authenticationProvider is IXRefreshableAuthenticationProvider refreshable)
                    {
                        hasRefreshedForThisCall = true;
                        await refreshable.ForceRefreshAsync(operationCts.Token).ConfigureAwait(false);
                        continue;
                    }

                    if (isRetryableMethod && !isLastAttempt && IsRetryableStatus(response.StatusCode))
                    {
                        var delay = response.StatusCode == HttpStatusCode.TooManyRequests
                            ? ComputeRateLimitDelay(response, rateLimit) ?? ComputeBackoffDelay(attempt)
                            : ComputeBackoffDelay(attempt);

                        await DelayAsync(delay, operationCts.Token, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw await BuildExceptionAsync(response, cancellationToken, operationCts.Token).ConfigureAwait(false);
                }

                return await readBody(response, headers, rateLimit, cancellationToken, operationCts.Token).ConfigureAwait(false);
            }
        }
    }

    private async Task<XResponse<TBody>> ReadJsonBodyAsync<TBody>(HttpResponseMessage response, IReadOnlyDictionary<string, IReadOnlyList<string>> headers, XRateLimitInfo? rateLimit, CancellationToken callerToken, CancellationToken operationToken)
    {
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

        if (response.Content.Headers.ContentLength is { } declaredLength && declaredLength > _options.MaxResponseBufferSize)
        {
            throw new XProtocolException(
                $"Response declared Content-Length {declaredLength} bytes, exceeding the configured maximum of {_options.MaxResponseBufferSize} bytes (XClientOptions.MaxResponseBufferSize).",
                response.StatusCode);
        }

        TBody? body;
        try
        {
            var rawStream = await response.Content.ReadAsStreamAsync(operationToken).ConfigureAwait(false);
            var stream = new MaxLengthStream(rawStream, _options.MaxResponseBufferSize);
            await using (stream.ConfigureAwait(false))
            {
                body = await JsonSerializer.DeserializeAsync<TBody>(stream, JsonOptions, operationToken).ConfigureAwait(false);
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
            throw ClassifyCancellation(ex, new CancellationContext(callerToken, operationToken, Attempt: null));
        }

        return new XResponse<TBody>
        {
            Body = body,
            StatusCode = response.StatusCode,
            Headers = headers,
            RateLimit = rateLimit,
            HasErrors = (body as IXErrorCarryingResponse)?.HasErrors ?? false,
            IsPartialSuccess = (body as IXErrorCarryingResponse)?.IsPartialSuccess ?? false,
        };
    }

    private async Task<XResponse<byte[]>> ReadBytesBodyAsync(HttpResponseMessage response, IReadOnlyDictionary<string, IReadOnlyList<string>> headers, XRateLimitInfo? rateLimit, CancellationToken callerToken, CancellationToken operationToken)
    {
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return new XResponse<byte[]>
            {
                Body = [],
                StatusCode = response.StatusCode,
                Headers = headers,
                RateLimit = rateLimit,
            };
        }

        if (response.Content.Headers.ContentLength is { } declaredLength && declaredLength > _options.MaxResponseBufferSize)
        {
            throw new XProtocolException(
                $"Response declared Content-Length {declaredLength} bytes, exceeding the configured maximum of {_options.MaxResponseBufferSize} bytes (XClientOptions.MaxResponseBufferSize).",
                response.StatusCode);
        }

        byte[] body;
        try
        {
            var rawStream = await response.Content.ReadAsStreamAsync(operationToken).ConfigureAwait(false);
            var stream = new MaxLengthStream(rawStream, _options.MaxResponseBufferSize);
            await using (stream.ConfigureAwait(false))
            {
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, operationToken).ConfigureAwait(false);
                body = buffer.ToArray();
            }
        }
        catch (OperationCanceledException ex)
        {
            throw ClassifyCancellation(ex, new CancellationContext(callerToken, operationToken, Attempt: null));
        }

        return new XResponse<byte[]>
        {
            Body = body,
            StatusCode = response.StatusCode,
            Headers = headers,
            RateLimit = rateLimit,
        };
    }

    /// <summary>
    /// Only GET/HEAD ever retry (spec 13.2: "POST/PATCH/DELETE ... по умолчанию не повторять
    /// автоматически"). This applies to a documented temporary 429 too, deliberately more
    /// conservative than the spec table's literal per-row reading: a write that returned 429
    /// might still have been applied server-side, and retrying it risks a duplicate write, which
    /// the spec treats as strictly worse than an extra failed read retry.
    /// </summary>
    private static bool IsRetryableStatus(HttpStatusCode status) => status is
        HttpStatusCode.InternalServerError or
        HttpStatusCode.BadGateway or
        HttpStatusCode.ServiceUnavailable or
        HttpStatusCode.GatewayTimeout or
        HttpStatusCode.TooManyRequests;

    /// <summary>RATE-01/06: prefer the server's own wait hint (Retry-After, then the rate-limit
    /// reset header) over guessing. Returns null only when neither is present/parsable.</summary>
    private TimeSpan? ComputeRateLimitDelay(HttpResponseMessage response, XRateLimitInfo? rateLimit)
    {
        if (response.Headers.RetryAfter is { } retryAfter)
        {
            if (retryAfter.Delta is { } delta)
            {
                return delta;
            }

            if (retryAfter.Date is { } date)
            {
                var now = _timeProvider.GetUtcNow();
                return date > now ? date - now : TimeSpan.Zero;
            }
        }

        if (rateLimit?.Reset is { } reset)
        {
            var now = _timeProvider.GetUtcNow();
            return reset > now ? reset - now : TimeSpan.Zero;
        }

        return null;
    }

    /// <summary>Exponential backoff capped at <see cref="XClientOptions.MaxRetryDelay"/>, with
    /// full jitter (spec: "jitter через контролируемый random" - <see cref="_jitterSource"/> is
    /// injectable so tests get deterministic delays).</summary>
    private TimeSpan ComputeBackoffDelay(int attemptNumber)
    {
        var exponential = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attemptNumber - 1));
        var capped = exponential > _options.MaxRetryDelay ? _options.MaxRetryDelay : exponential;
        return capped * _jitterSource.NextDouble();
    }

    /// <summary>RATE-06: the wait before a retry is cancellable and bounded by the same
    /// operation deadline as everything else - it is not a separate, unbounded sleep.</summary>
    private async Task DelayAsync(TimeSpan delay, CancellationToken operationToken, CancellationToken callerToken)
    {
        try
        {
            await Task.Delay(delay, _timeProvider, operationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw ClassifyCancellation(ex, new CancellationContext(callerToken, operationToken, Attempt: null));
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

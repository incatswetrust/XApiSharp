using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>
/// Closes real branch-coverage gaps in <see cref="RequestExecutor"/> (spec section 19.5/E6: "≥80%
/// branch coverage on hand-written core") that the family-client contract tests never exercise -
/// transport-level failures, the oversized-response guard, the untested 403/429-after-retries
/// exception shapes, Retry-After's date form, the rate-limit-reset fallback, and
/// <see cref="RequestExecutor.OpenStreamAsync"/>'s own failure/refresh/null-query paths (the
/// Streaming contract tests only ever exercise its success path).
/// </summary>
public class RequestExecutorCoreTests
{
    [Fact]
    public async Task Get_retries_after_a_thrown_HttpRequestException_then_succeeds()
    {
        var timeProvider = new FakeTimeProvider();
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new HttpRequestException("Connection refused.");
            }

            return Task.FromResult(SuccessResponse());
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(1));

        await using var pump = StartPump(timeProvider);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(2, callCount);
        Assert.Equal("A", response.Body?.Data?.Name);
    }

    [Fact]
    public async Task Post_never_retries_a_thrown_HttpRequestException_and_wraps_it()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => throw new HttpRequestException("Connection refused."));
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        await Assert.ThrowsAsync<XTransportException>(() => executor.SendAsync<GetUserResponse>(HttpMethod.Post, "2/test", CancellationToken.None));
    }

    [Fact]
    public async Task Get_exhausts_retries_on_429_and_throws_XRateLimitException_not_a_generic_error()
    {
        var timeProvider = new FakeTimeProvider();
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxRetries = 1 };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options, timeProvider, new Random(1));

        await using var pump = StartPump(timeProvider);

        var ex = await Assert.ThrowsAsync<XRateLimitException>(() => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Equal(HttpStatusCode.TooManyRequests, ex.StatusCode);
    }

    [Fact]
    public async Task A_403_response_is_reported_as_XAccessDeniedException()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        var ex = await Assert.ThrowsAsync<XAccessDeniedException>(() => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
    }

    [Fact]
    public async Task An_error_response_with_a_non_problem_content_type_falls_back_to_a_generic_message()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("<html>server error</html>", Encoding.UTF8, "text/html"),
        }));
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxRetries = 0 };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options);

        var ex = await Assert.ThrowsAsync<XApiException>(() => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Null(ex.Problem);
        Assert.Contains("500", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_honors_a_Retry_After_header_given_as_an_HTTP_date()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(timeProvider.GetUtcNow().AddSeconds(10));
                return Task.FromResult(response);
            }

            return Task.FromResult(SuccessResponse());
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(1));

        await using var pump = StartPump(timeProvider);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(2, callCount);
        Assert.Equal("A", response.Body?.Data?.Name);
    }

    [Fact]
    public async Task A_429_without_Retry_After_falls_back_to_the_rate_limit_reset_header()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.Add("x-rate-limit-limit", "1");
                response.Headers.Add("x-rate-limit-remaining", "0");
                response.Headers.Add("x-rate-limit-reset", timeProvider.GetUtcNow().AddSeconds(5).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
                return Task.FromResult(response);
            }

            return Task.FromResult(SuccessResponse());
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(1));

        await using var pump = StartPump(timeProvider);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(2, callCount);
        Assert.Equal("A", response.Body?.Data?.Name);
    }

    [Fact]
    public async Task A_declared_Content_Length_over_the_configured_maximum_throws_without_buffering()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(new string('x', 100), Encoding.UTF8, "application/json"),
            };
            response.Content.Headers.ContentLength = 10_000_000; // lies about the real body size
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxResponseBufferSize = 1024 };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options);

        var ex = await Assert.ThrowsAsync<XProtocolException>(() => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Contains("MaxResponseBufferSize", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_oversized_declared_Content_Length_on_a_binary_response_also_throws()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) };
            response.Content.Headers.ContentLength = 10_000_000;
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxResponseBufferSize = 1024 };
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), options, TimeProvider.System, new Random(1));

        await Assert.ThrowsAsync<XProtocolException>(() => executor.SendForBytesAsync(HttpMethod.Get, "2/test", CancellationToken.None));
    }

    [Fact]
    public async Task A_no_content_response_to_a_binary_request_returns_an_empty_array()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        var response = await executor.SendForBytesAsync(HttpMethod.Get, "2/test", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(response.Body!);
    }

    [Fact]
    public async Task OpenStreamAsync_omits_the_query_string_when_no_parameters_are_given()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(string.Empty, request.RequestUri!.Query);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        });
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        using var response = await executor.OpenStreamAsync(HttpMethod.Get, "2/test/stream", queryParameters: null, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenStreamAsync_maps_a_non_success_status_to_the_same_exception_hierarchy()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        await Assert.ThrowsAsync<XAccessDeniedException>(() => executor.OpenStreamAsync(HttpMethod.Get, "2/test/stream", queryParameters: null, CancellationToken.None));
    }

    [Fact]
    public async Task OpenStreamAsync_refreshes_once_on_401_then_succeeds()
    {
        var refreshCount = 0;
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") });
        });
        using var httpClient = new HttpClient(handler);
        var auth = new RefreshableStubAuthProvider(() => refreshCount++);
        var executor = new RequestExecutor(httpClient, auth, new XClientOptions(), TimeProvider.System, new Random(1));

        using var response = await executor.OpenStreamAsync(HttpMethod.Get, "2/test/stream", queryParameters: null, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, refreshCount);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task OpenStreamAsync_reports_an_external_HttpClient_Timeout_the_same_way_ExecuteAsync_does()
    {
        // HTTP-08: OpenStreamAsync has no separate operation/attempt deadline of its own (see its
        // own doc comment), but it must still recognize the BCL's own TaskCanceledException{
        // InnerException: TimeoutException} shape from an external HttpClient.Timeout the same
        // way the regular Send*Async path does, not let it leak out raw.
        using var handler = new FakeHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException()));
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        var ex = await Assert.ThrowsAsync<XRequestTimeoutException>(() => executor.OpenStreamAsync(HttpMethod.Get, "2/test/stream", queryParameters: null, CancellationToken.None));

        Assert.Contains("HttpClient.Timeout", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenStreamAsync_propagates_caller_cancellation_as_is()
    {
        using var cts = new CancellationTokenSource();
        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            await cts.CancelAsync();
            await Task.Delay(Timeout.Infinite, ct);
            throw new InvalidOperationException("unreachable - Task.Delay(Timeout.Infinite) should have observed the cancellation above");
        });
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.OpenStreamAsync(HttpMethod.Get, "2/test/stream", queryParameters: null, cts.Token));
    }

    private static HttpResponseMessage SuccessResponse() => new(HttpStatusCode.OK)
    {
        Content = new StringContent("""{"data":{"id":"1","name":"A","username":"a"}}""", Encoding.UTF8, "application/json"),
    };

    private static Pump StartPump(FakeTimeProvider timeProvider)
    {
        var cts = new CancellationTokenSource();
        var pumpTask = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                timeProvider.Advance(TimeSpan.FromSeconds(1));
                await Task.Delay(1, CancellationToken.None).ConfigureAwait(false);
            }
        });

        return new Pump(cts, pumpTask);
    }

    private sealed class Pump(CancellationTokenSource cts, Task pumpTask) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await cts.CancelAsync();
            await pumpTask;
            cts.Dispose();
        }
    }

    private sealed class RefreshableStubAuthProvider(Action onRefresh) : IXAuthenticationProvider, IXRefreshableAuthenticationProvider
    {
        public ValueTask PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token");
            return ValueTask.CompletedTask;
        }

        public Task ForceRefreshAsync(CancellationToken cancellationToken)
        {
            onRefresh();
            return Task.CompletedTask;
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>Spec section 13.2 default retry policy + RATE-01/06. All timing goes through
/// FakeTimeProvider, driven by a background "pump" that advances it - no real multi-second
/// waits (spec: "every timeout-dependent test goes through TimeProvider").</summary>
public class RetryTests
{
    [Fact]
    public async Task Get_retries_a_transient_5xx_then_succeeds()
    {
        var timeProvider = new FakeTimeProvider();
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount < 3)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            return Task.FromResult(SuccessResponse());
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(42));

        await using var pump = StartPump(timeProvider);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(3, callCount);
        Assert.Equal("A", response.Body?.Data?.Name);
    }

    [Fact]
    public async Task Get_exhausts_retries_and_throws_the_last_error()
    {
        var timeProvider = new FakeTimeProvider();
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { MaxRetries = 2 };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options, timeProvider, new Random(42));

        await using var pump = StartPump(timeProvider);

        await Assert.ThrowsAsync<XApiException>(() => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Equal(3, callCount); // initial attempt + 2 retries, then give up
    }

    [Fact]
    public async Task Post_never_retries_a_transient_5xx()
    {
        // No public write endpoint exists yet (E4) - exercise the shared RequestExecutor
        // directly (internal, visible to this test project) to prove the write-safety rule.
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(
            httpClient,
            new BearerTokenAuthenticationProvider("t"),
            new XClientOptions { MaxRetries = 2 },
            TimeProvider.System,
            new Random(42));

        await Assert.ThrowsAsync<XApiException>(() => executor.SendAsync<GetUserResponse>(HttpMethod.Post, "2/test", CancellationToken.None));

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Get_honors_the_Retry_After_header_for_429_then_succeeds()
    {
        var timeProvider = new FakeTimeProvider();
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
                return Task.FromResult(response);
            }

            return Task.FromResult(SuccessResponse());
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(42));

        await using var pump = StartPump(timeProvider);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(2, callCount);
        Assert.Equal("A", response.Body?.Data?.Name);
    }

    [Fact]
    public async Task Rate_limit_wait_that_would_exceed_the_operation_deadline_gives_up_instead_of_hanging()
    {
        // RATE-06: the wait is bounded by the overall operation deadline, not just "eventually".
        var timeProvider = new FakeTimeProvider();
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(10));
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var options = new XClientOptions { OperationTimeout = TimeSpan.FromSeconds(5) };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options, timeProvider, new Random(42));

        await using var pump = StartPump(timeProvider);

        var ex = await Assert.ThrowsAsync<XRequestTimeoutException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Contains("OperationTimeout", ex.Message, StringComparison.Ordinal);
    }

    private static HttpResponseMessage SuccessResponse()
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"data":{"id":"1","name":"A","username":"a"}}""", Encoding.UTF8, "application/json"),
        };
    }

    /// <summary>Repeatedly advances the fake clock so any pending FakeTimeProvider-backed delay
    /// or CancellationTokenSource timer completes, without a real multi-second wait.</summary>
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
}

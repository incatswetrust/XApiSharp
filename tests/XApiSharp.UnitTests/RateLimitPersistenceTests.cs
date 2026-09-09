using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>
/// End-to-end coverage of RATE-03/04/05 through the real <see cref="XApiClient"/>/
/// <see cref="Transport.RequestExecutor"/> path - the pure ordering/scoping logic itself is
/// covered more directly (and deterministically) in <c>XRateLimitContextStoreTests</c>; these
/// tests prove that logic is actually wired into the request path the way a caller would observe
/// it: a known-exhausted scope makes the *next* call wait before sending, and that waiting never
/// crosses between separate auth contexts.
/// </summary>
public class RateLimitPersistenceTests
{
    [Fact]
    public async Task A_known_exhausted_scope_makes_the_next_call_wait_for_reset_before_sending()
    {
        var timeProvider = new FakeTimeProvider();
        var resetAt = timeProvider.GetUtcNow().AddSeconds(5);
        var callCount = 0;
        var sendTimestamps = new List<DateTimeOffset>();

        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            sendTimestamps.Add(timeProvider.GetUtcNow());

            var response = SuccessResponse();
            if (callCount == 1)
            {
                response.Headers.Add("x-rate-limit-limit", "1");
                response.Headers.Add("x-rate-limit-remaining", "0");
                response.Headers.Add("x-rate-limit-reset", resetAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(1));

        await using var pump = StartPump(timeProvider);

        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });
        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(2, callCount);
        Assert.True(sendTimestamps[1] >= resetAt, $"second send at {sendTimestamps[1]:O} should not precede the recorded reset at {resetAt:O}");
    }

    [Fact]
    public async Task RATE04_one_clients_exhausted_scope_never_delays_a_different_auth_contexts_calls()
    {
        var timeProvider = new FakeTimeProvider();
        var resetAt = timeProvider.GetUtcNow().AddMinutes(5);

        using var exhaustingHandler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = SuccessResponse();
            response.Headers.Add("x-rate-limit-limit", "1");
            response.Headers.Add("x-rate-limit-remaining", "0");
            response.Headers.Add("x-rate-limit-reset", resetAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
            return Task.FromResult(response);
        });
        using var exhaustingHttpClient = new HttpClient(exhaustingHandler);
        var exhaustedClient = new XApiClient(exhaustingHttpClient, new BearerTokenAuthenticationProvider("user-a-token"), timeProvider: timeProvider, retryJitterSource: new Random(1));
        await exhaustedClient.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        // A different auth context (a different provider instance - "user B") hitting the exact
        // same route must not see user A's exhausted state at all.
        var otherCallCompleted = false;
        using var otherHandler = new FakeHttpMessageHandler((_, _) =>
        {
            otherCallCompleted = true;
            return Task.FromResult(SuccessResponse());
        });
        using var otherHttpClient = new HttpClient(otherHandler);
        var otherClient = new XApiClient(otherHttpClient, new BearerTokenAuthenticationProvider("user-b-token"), timeProvider: timeProvider, retryJitterSource: new Random(1));

        // No pump running: if this call incorrectly waited on user A's 5-minute reset, it would
        // never complete and this test would time out instead of passing.
        await otherClient.Users.GetByIdAsync(new GetUserRequest { Id = "1" }).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(otherCallCompleted);
    }

    [Fact]
    public async Task RATE02_a_response_without_rate_limit_headers_does_not_clear_previously_known_exhaustion()
    {
        var timeProvider = new FakeTimeProvider();
        var resetAt = timeProvider.GetUtcNow().AddSeconds(5);
        var callCount = 0;
        var sendTimestamps = new List<DateTimeOffset>();

        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            sendTimestamps.Add(timeProvider.GetUtcNow());

            // Only the first response carries rate-limit headers; the second (a plain success,
            // no headers at all) must not be read as "quota restored" before the real reset.
            if (callCount == 1)
            {
                var response = SuccessResponse();
                response.Headers.Add("x-rate-limit-limit", "1");
                response.Headers.Add("x-rate-limit-remaining", "0");
                response.Headers.Add("x-rate-limit-reset", resetAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture));
                return Task.FromResult(response);
            }

            return Task.FromResult(SuccessResponse());
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider, retryJitterSource: new Random(1));

        await using var pump = StartPump(timeProvider);

        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });
        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }); // headerless response
        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }); // must still have waited for the original reset

        Assert.Equal(3, callCount);
        Assert.True(sendTimestamps[1] >= resetAt, "the second send should already have waited for the original reset");
        Assert.True(sendTimestamps[2] >= resetAt, "the third send must not be sent early just because the second response had no rate-limit headers");
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
}

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Errors;

namespace XApiSharp.UnitTests;

public class XOAuth2UserAuthenticationProviderTests
{
    [Fact]
    public async Task PrepareRequestAsync_attaches_the_seeded_token_without_refreshing()
    {
        var refreshCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) => { refreshCount++; return Task.FromResult(TokenResponse("should-not-be-used")); });
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id");
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client);
        await provider.SetInitialTokenAsync(new XOAuth2TokenResponse { AccessToken = "seeded-token", TokenType = "bearer", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) }, CancellationToken.None);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await provider.PrepareRequestAsync(request, CancellationToken.None);

        Assert.Equal("seeded-token", request.Headers.Authorization?.Parameter);
        Assert.Equal(0, refreshCount);
    }

    [Fact]
    public async Task Token_within_the_expiry_buffer_triggers_a_refresh()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(TokenResponse("refreshed-token")));
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id", timeProvider: timeProvider);
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client, timeProvider: timeProvider);
        await provider.SetInitialTokenAsync(
            new XOAuth2TokenResponse { AccessToken = "old-token", RefreshToken = "r", TokenType = "bearer", ExpiresAtUtc = timeProvider.GetUtcNow().AddSeconds(10) },
            CancellationToken.None);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await provider.PrepareRequestAsync(request, CancellationToken.None);

        Assert.Equal("refreshed-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Fifty_concurrent_calls_with_an_expired_token_produce_exactly_one_refresh()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var refreshCount = 0;
        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            Interlocked.Increment(ref refreshCount);
            await Task.Delay(15, ct); // widen the race window
            return TokenResponse("refreshed-token");
        });
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id", timeProvider: timeProvider);
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client, timeProvider: timeProvider);
        await provider.SetInitialTokenAsync(
            new XOAuth2TokenResponse { AccessToken = "old", RefreshToken = "r", TokenType = "bearer", ExpiresAtUtc = timeProvider.GetUtcNow() },
            CancellationToken.None);

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.x.com/2/users/{i}");
            await provider.PrepareRequestAsync(request, CancellationToken.None);
            return request.Headers.Authorization?.Parameter;
        });
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, refreshCount);
        Assert.All(results, r => Assert.Equal("refreshed-token", r));
    }

    [Fact]
    public async Task Cancelling_one_waiting_caller_does_not_abort_the_shared_refresh_for_the_rest()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var refreshStarted = new TaskCompletionSource();
        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            refreshStarted.TrySetResult();
            await Task.Delay(50, ct);
            return TokenResponse("refreshed-token");
        });
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id", timeProvider: timeProvider);
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client, timeProvider: timeProvider);
        await provider.SetInitialTokenAsync(
            new XOAuth2TokenResponse { AccessToken = "old", RefreshToken = "r", TokenType = "bearer", ExpiresAtUtc = timeProvider.GetUtcNow() },
            CancellationToken.None);

        using var cts = new CancellationTokenSource();
        using var requestA = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        var taskA = provider.PrepareRequestAsync(requestA, cts.Token).AsTask();

        await refreshStarted.Task; // the shared refresh is now in flight
        await cts.CancelAsync(); // cancel the caller that happens to be driving it

        using var requestB = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/2");
        await provider.PrepareRequestAsync(requestB, CancellationToken.None);

        Assert.Equal("refreshed-token", requestB.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Invalid_grant_on_refresh_clears_the_refresh_token_so_the_next_call_fails_fast_without_a_second_network_call()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":"invalid_grant","error_description":"Token is invalid or expired"}""", Encoding.UTF8, "application/json"),
            });
        });
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id", timeProvider: timeProvider);
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client, timeProvider: timeProvider);
        await provider.SetInitialTokenAsync(
            new XOAuth2TokenResponse { AccessToken = "old", RefreshToken = "r", TokenType = "bearer", ExpiresAtUtc = timeProvider.GetUtcNow() },
            CancellationToken.None);

        using var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await Assert.ThrowsAsync<XAuthenticationException>(() => provider.PrepareRequestAsync(request1, CancellationToken.None).AsTask());
        Assert.Equal(1, callCount);

        // AUTH-10: the second call must not attempt another network refresh - it should fail
        // immediately because the (now known-dead) refresh token was cleared.
        using var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/2");
        await Assert.ThrowsAsync<XAuthenticationException>(() => provider.PrepareRequestAsync(request2, CancellationToken.None).AsTask());
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Two_provider_instances_never_share_tokens()
    {
        using var httpClient1 = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(TokenResponse("should-not-be-called"))));
        using var httpClient2 = new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(TokenResponse("should-not-be-called"))));
        var client1 = new XOAuth2Client(httpClient1, "client-id");
        var client2 = new XOAuth2Client(httpClient2, "client-id");
        using var providerA = new XOAuth2UserAuthenticationProvider(client1);
        using var providerB = new XOAuth2UserAuthenticationProvider(client2);
        await providerA.SetInitialTokenAsync(new XOAuth2TokenResponse { AccessToken = "token-a", TokenType = "bearer", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) }, CancellationToken.None);
        await providerB.SetInitialTokenAsync(new XOAuth2TokenResponse { AccessToken = "token-b", TokenType = "bearer", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) }, CancellationToken.None);

        using var requestA = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await providerA.PrepareRequestAsync(requestA, CancellationToken.None);
        using var requestB = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await providerB.PrepareRequestAsync(requestB, CancellationToken.None);

        Assert.Equal("token-a", requestA.Headers.Authorization?.Parameter);
        Assert.Equal("token-b", requestB.Headers.Authorization?.Parameter);
    }

    private static HttpResponseMessage TokenResponse(string accessToken)
    {
        var json = JsonSerializer.Serialize(new { token_type = "bearer", access_token = accessToken, expires_in = 7200L, refresh_token = "new-refresh-token" });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

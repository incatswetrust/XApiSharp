using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>Spec section 13.2 retry table: "Expired OAuth 2.0 access token | No more than one
/// refresh; resending only in an unambiguously safe scenario" - exercised through
/// XApiClient + XOAuth2UserAuthenticationProvider together, since the behavior lives in
/// RequestExecutor's cooperation with IXRefreshableAuthenticationProvider.</summary>
public class RequestExecutorAuthRefreshTests
{
    [Fact]
    public async Task A_401_triggers_exactly_one_refresh_then_the_retried_request_succeeds()
    {
        var apiCallCount = 0;
        var refreshCallCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/2/oauth2/token")
            {
                refreshCallCount++;
                return Task.FromResult(TokenResponse("refreshed-token"));
            }

            apiCallCount++;
            if (apiCallCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            Assert.Equal("Bearer refreshed-token", request.Headers.Authorization?.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{"id":"1","name":"A","username":"a"}}""", Encoding.UTF8, "application/json"),
            });
        });
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id");
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client);
        await provider.SetInitialTokenAsync(
            new XOAuth2TokenResponse { AccessToken = "stale-token", RefreshToken = "r", TokenType = "bearer", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) },
            CancellationToken.None);
        var client = new XApiClient(httpClient, provider);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(2, apiCallCount);
        Assert.Equal(1, refreshCallCount);
        Assert.Equal("A", response.Body?.Data?.Name);
    }

    [Fact]
    public async Task A_second_401_after_the_single_refresh_does_not_loop()
    {
        var apiCallCount = 0;
        var refreshCallCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/2/oauth2/token")
            {
                refreshCallCount++;
                return Task.FromResult(TokenResponse("refreshed-token"));
            }

            apiCallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        using var httpClient = new HttpClient(handler);
        var oauth2Client = new XOAuth2Client(httpClient, "client-id");
        using var provider = new XOAuth2UserAuthenticationProvider(oauth2Client);
        await provider.SetInitialTokenAsync(
            new XOAuth2TokenResponse { AccessToken = "stale-token", RefreshToken = "r", TokenType = "bearer", ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) },
            CancellationToken.None);
        var client = new XApiClient(httpClient, provider);

        await Assert.ThrowsAsync<XAuthenticationException>(() => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Equal(2, apiCallCount); // original attempt + one retry after refresh, no more
        Assert.Equal(1, refreshCallCount); // exactly one refresh, not one per 401
    }

    private static HttpResponseMessage TokenResponse(string accessToken)
    {
        var json = JsonSerializer.Serialize(new { token_type = "bearer", access_token = accessToken, expires_in = 7200L, refresh_token = "new-refresh-token" });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}

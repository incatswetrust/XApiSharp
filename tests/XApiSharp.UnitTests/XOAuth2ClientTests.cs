using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Errors;

namespace XApiSharp.UnitTests;

public class XOAuth2ClientTests
{
    private static readonly Uri RedirectUri = new("https://app.example.com/callback");

    [Fact]
    public void CreateAuthorizationRequest_builds_the_documented_url_shape()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var client = new XOAuth2Client(new HttpClient(), "my-client-id", timeProvider: timeProvider);

        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read", "users.read"]);

        Assert.Equal("x.com", authRequest.AuthorizationUrl.Host);
        Assert.Equal("/i/oauth2/authorize", authRequest.AuthorizationUrl.AbsolutePath);

        var query = ParseQuery(authRequest.AuthorizationUrl.Query);
        Assert.Equal("code", query["response_type"]);
        Assert.Equal("my-client-id", query["client_id"]);
        Assert.Equal(RedirectUri.ToString(), query["redirect_uri"]);
        Assert.Equal("tweet.read users.read", query["scope"]);
        Assert.Equal(authRequest.State, query["state"]);
        Assert.Equal("S256", query["code_challenge_method"]);

        // AUTH-01: the challenge is the base64url(SHA256(verifier)) - verify the actual PKCE math.
        var expectedChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(authRequest.CodeVerifier)));
        Assert.Equal(expectedChallenge, query["code_challenge"]);

        Assert.Equal(timeProvider.GetUtcNow() + TimeSpan.FromMinutes(10), authRequest.ExpiresAtUtc);
    }

    [Fact]
    public void CreateAuthorizationRequest_produces_a_different_state_and_verifier_each_call()
    {
        var client = new XOAuth2Client(new HttpClient(), "client-id");

        var first = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);
        var second = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        Assert.NotEqual(first.State, second.State);
        Assert.NotEqual(first.CodeVerifier, second.CodeVerifier);
    }

    [Fact]
    public async Task CompleteAuthorizationAsync_rejects_an_expired_session_without_calling_the_server()
    {
        var timeProvider = new FakeTimeProvider();
        var called = false;
        using var handler = new FakeHttpMessageHandler((_, _) => { called = true; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });
        using var httpClient = new HttpClient(handler);
        var client = new XOAuth2Client(httpClient, "client-id", timeProvider: timeProvider);
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"], TimeSpan.FromMinutes(1));

        timeProvider.Advance(TimeSpan.FromMinutes(2));

        await Assert.ThrowsAsync<XAuthenticationException>(
            () => client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=abc&state={authRequest.State}"), CancellationToken.None));

        Assert.False(called);
    }

    [Fact]
    public async Task CompleteAuthorizationAsync_rejects_a_state_mismatch()
    {
        var client = new XOAuth2Client(new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))), "client-id");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        await Assert.ThrowsAsync<XAuthenticationException>(
            () => client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=abc&state=wrong-state"), CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAuthorizationAsync_surfaces_the_documented_oauth_callback_error()
    {
        var client = new XOAuth2Client(new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))), "client-id");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        var ex = await Assert.ThrowsAsync<XAuthenticationException>(() => client.CompleteAuthorizationAsync(
            authRequest,
            new Uri($"{RedirectUri}?error=access_denied&error_description=User%20denied%20access&state={authRequest.State}"),
            CancellationToken.None));

        Assert.Contains("access_denied", ex.Message, StringComparison.Ordinal);
        Assert.Contains("User denied access", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompleteAuthorizationAsync_rejects_a_callback_at_a_different_uri()
    {
        var client = new XOAuth2Client(new HttpClient(new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)))), "client-id");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        await Assert.ThrowsAsync<XAuthenticationException>(() => client.CompleteAuthorizationAsync(
            authRequest,
            new Uri($"https://evil.example.com/callback?code=abc&state={authRequest.State}"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Confidential_client_authenticates_via_basic_auth_header_not_body_client_id()
    {
        HttpRequestMessage? seenRequest = null;
        string? seenBody = null;
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            seenRequest = request;
            seenBody = await request.Content!.ReadAsStringAsync(ct);
            return TokenResponse();
        });
        using var httpClient = new HttpClient(handler);
        var client = new XOAuth2Client(httpClient, "client-id", "client-secret");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        await client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=the-code&state={authRequest.State}"), CancellationToken.None);

        Assert.Equal("Basic", seenRequest!.Headers.Authorization?.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.ASCII.GetBytes("client-id:client-secret")),
            seenRequest.Headers.Authorization?.Parameter);
        Assert.DoesNotContain("client_id", seenBody, StringComparison.Ordinal);
        Assert.Contains("grant_type=authorization_code", seenBody, StringComparison.Ordinal);
        Assert.Contains("code=the-code", seenBody, StringComparison.Ordinal);
        Assert.Contains($"code_verifier={authRequest.CodeVerifier}", seenBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_client_authenticates_via_client_id_in_the_body_not_basic_auth()
    {
        HttpRequestMessage? seenRequest = null;
        string? seenBody = null;
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            seenRequest = request;
            seenBody = await request.Content!.ReadAsStringAsync(ct);
            return TokenResponse();
        });
        using var httpClient = new HttpClient(handler);
        var client = new XOAuth2Client(httpClient, "client-id");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        await client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=the-code&state={authRequest.State}"), CancellationToken.None);

        Assert.Null(seenRequest!.Headers.Authorization);
        Assert.Contains("client_id=client-id", seenBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_exchange_computes_expiry_from_the_server_response()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(TokenResponse(expiresIn: 7200, scope: "tweet.read users.read", refreshToken: "r-token")));
        using var httpClient = new HttpClient(handler);
        var client = new XOAuth2Client(httpClient, "client-id", timeProvider: timeProvider);
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read", "users.read"]);

        var token = await client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=c&state={authRequest.State}"), CancellationToken.None);

        Assert.Equal("the-access-token", token.AccessToken);
        Assert.Equal("r-token", token.RefreshToken);
        Assert.Equal(timeProvider.GetUtcNow() + TimeSpan.FromHours(2), token.ExpiresAtUtc);
        Assert.Equal(["tweet.read", "users.read"], token.Scopes);
    }

    [Fact]
    public async Task Missing_expires_in_is_a_protocol_error_not_a_guessed_default()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"token_type":"bearer","access_token":"a"}""", Encoding.UTF8, "application/json"),
        }));
        using var httpClient = new HttpClient(handler);
        var client = new XOAuth2Client(httpClient, "client-id");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        await Assert.ThrowsAsync<XProtocolException>(
            () => client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=c&state={authRequest.State}"), CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_grant_maps_to_XAuthenticationException_with_the_error_code_in_the_message()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"invalid_grant","error_description":"Value passed for the authorization code was invalid."}""", Encoding.UTF8, "application/json"),
        }));
        using var httpClient = new HttpClient(handler);
        var client = new XOAuth2Client(httpClient, "client-id");
        var authRequest = client.CreateAuthorizationRequest(RedirectUri, ["tweet.read"]);

        var ex = await Assert.ThrowsAsync<XAuthenticationException>(
            () => client.CompleteAuthorizationAsync(authRequest, new Uri($"{RedirectUri}?code=c&state={authRequest.State}"), CancellationToken.None));

        Assert.Contains("invalid_grant", ex.Message, StringComparison.Ordinal);
    }

    private static HttpResponseMessage TokenResponse(long expiresIn = 7200, string? scope = null, string? refreshToken = null)
    {
        var json = JsonSerializer.Serialize(new
        {
            token_type = "bearer",
            access_token = "the-access-token",
            expires_in = expiresIn,
            scope,
            refresh_token = refreshToken,
        });
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var collection = HttpUtility.ParseQueryString(query);
        return collection.AllKeys.Where(k => k is not null).ToDictionary(k => k!, k => collection[k]!);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

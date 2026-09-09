using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

public class XAppOnlyAuthenticationProviderTests
{
    [Fact]
    public async Task First_use_fetches_a_token_with_the_documented_contract()
    {
        HttpRequestMessage? seenRequest = null;
        string? seenBody = null;
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            seenRequest = request;
            seenBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return TokenResponse("the-token");
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "consumer-key", "consumer-secret");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await provider.PrepareRequestAsync(request, CancellationToken.None);

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("the-token", request.Headers.Authorization?.Parameter);

        Assert.Equal(HttpMethod.Post, seenRequest!.Method);
        Assert.Equal("/oauth2/token", seenRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Basic", seenRequest.Headers.Authorization?.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.ASCII.GetBytes("consumer-key:consumer-secret")),
            seenRequest.Headers.Authorization?.Parameter);
        Assert.Equal("grant_type=client_credentials", seenBody);
        Assert.Equal("application/x-www-form-urlencoded", seenRequest.Content!.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Second_use_reuses_the_cached_token_without_a_second_fetch()
    {
        var fetchCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            fetchCount++;
            return Task.FromResult(TokenResponse("the-token"));
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");

        using var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await provider.PrepareRequestAsync(request1, CancellationToken.None);
        using var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/2");
        await provider.PrepareRequestAsync(request2, CancellationToken.None);

        Assert.Equal(1, fetchCount);
        Assert.Equal("the-token", request2.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Concurrent_first_use_triggers_exactly_one_fetch()
    {
        var fetchCount = 0;
        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            Interlocked.Increment(ref fetchCount);
            await Task.Delay(20, ct); // widen the race window so concurrent callers actually overlap
            return TokenResponse("the-token");
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");

        var tasks = Enumerable.Range(0, 20).Select(async i =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.x.com/2/users/{i}");
            await provider.PrepareRequestAsync(request, CancellationToken.None);
            return request.Headers.Authorization?.Parameter;
        });

        var tokens = await Task.WhenAll(tasks);

        Assert.Equal(1, fetchCount);
        Assert.All(tokens, t => Assert.Equal("the-token", t));
    }

    [Fact]
    public async Task A_failed_fetch_maps_to_XAuthenticationException_with_the_legacy_error_message()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(
                    """{"errors":[{"code":99,"label":"authenticity_token_error","message":"Unable to verify your credentials"}]}""",
                    Encoding.UTF8,
                    "application/json"),
            };
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "bad-key", "bad-secret");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        var ex = await Assert.ThrowsAsync<XAuthenticationException>(
            () => provider.PrepareRequestAsync(request, CancellationToken.None).AsTask());

        Assert.Equal("Unable to verify your credentials", ex.Message);
        // Secrets never end up in an exception (spec section 10.3).
        Assert.DoesNotContain("bad-key", ex.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("bad-secret", ex.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task End_to_end_through_XApiClient_the_fetched_token_is_used_for_the_real_call()
    {
        string? apiCallAuthorization = null;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/oauth2/token")
            {
                return Task.FromResult(TokenResponse("fetched-token"));
            }

            apiCallAuthorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{"id":"1","name":"A","username":"a"}}""", Encoding.UTF8, "application/json"),
            });
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");
        var client = new XApiClient(httpClient, provider);

        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal("Bearer fetched-token", apiCallAuthorization);
    }

    [Fact]
    public async Task RefreshAsync_without_a_cached_token_just_fetches_one()
    {
        var revokeCalled = false;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri!.AbsolutePath == "/oauth2/invalidate_token")
            {
                revokeCalled = true;
            }

            return Task.FromResult(TokenResponse("fresh-token"));
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");

        var token = await provider.RefreshAsync(CancellationToken.None);

        Assert.Equal("fresh-token", token);
        Assert.False(revokeCalled);
    }

    [Fact]
    public async Task RefreshAsync_with_a_cached_token_revokes_it_first_then_fetches_a_new_one()
    {
        var revokedToken = (string?)null;
        var fetchCount = 0;
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath == "/oauth2/invalidate_token")
            {
                var form = await request.Content!.ReadAsStringAsync(ct);
                revokedToken = Uri.UnescapeDataString(form.Split('=')[1]);
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
            }

            fetchCount++;
            return TokenResponse($"token-{fetchCount}");
        });
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");

        // Populate the cache first.
        using var initialRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await provider.PrepareRequestAsync(initialRequest, CancellationToken.None);

        var refreshed = await provider.RefreshAsync(CancellationToken.None);

        Assert.Equal("token-1", initialRequest.Headers.Authorization?.Parameter);
        Assert.Equal("token-2", refreshed);
        Assert.Equal("token-1", revokedToken);
    }

    [Fact]
    public async Task A_response_missing_the_documented_token_shape_throws_XProtocolException()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"token_type":"mac","access_token":"x"}""", Encoding.UTF8, "application/json"),
        }));
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await Assert.ThrowsAsync<XProtocolException>(() => provider.PrepareRequestAsync(request, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task A_failed_fetch_with_no_parseable_error_body_falls_back_to_a_generic_message()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("<html>down for maintenance</html>", Encoding.UTF8, "text/html"),
        }));
        using var httpClient = new HttpClient(handler);
        using var provider = new XAppOnlyAuthenticationProvider(httpClient, "k", "s");

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        var ex = await Assert.ThrowsAsync<XAuthenticationException>(() => provider.PrepareRequestAsync(request, CancellationToken.None).AsTask());

        Assert.Contains("Failed to obtain the app-only token", ex.Message, StringComparison.Ordinal);
        Assert.Contains("503", ex.Message, StringComparison.Ordinal);
    }

    private static HttpResponseMessage TokenResponse(string accessToken)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"token_type":"bearer","access_token":"{{accessToken}}"}""", Encoding.UTF8, "application/json"),
        };
    }
}

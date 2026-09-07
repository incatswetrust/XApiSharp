using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for GET /2/users/{id} (registry key in spec/endpoint-manifest.json;
/// stable method name Users.GetByIdAsync in spec/method-name-map.json), per spec section 19.3.
/// </summary>
public class UsersGetByIdContractTests
{
    [Fact]
    public async Task Sends_correct_method_and_path_with_id_substituted()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/2/users/123456789", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"123456789","name":"Ada","username":"ada"}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetByIdAsync(new GetUserRequest { Id = "123456789" });
    }

    [Fact]
    public async Task Escapes_id_without_double_encoding()
    {
        // SER-10: exercise a value that needs escaping, and confirm it isn't escaped twice.
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/abc%2Fdef", request.RequestUri!.AbsoluteUri[(request.RequestUri.AbsoluteUri.IndexOf("/2/users/", StringComparison.Ordinal))..]);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"abc/def","name":"Test","username":"test"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "abc/def" });

        Assert.Equal("abc/def", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task Uses_bearer_authorization_from_the_configured_provider()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","name":"A","username":"a"}}"""));
        });
        var client = CreateClient(handler, token: "test-token");

        await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });
    }

    [Fact]
    public async Task Deserializes_the_documented_success_body_shape()
    {
        const string json = """
            {
              "data": {
                "id": "2244994945",
                "name": "X Dev",
                "username": "XDevelopers",
                "protected": false,
                "verified": true,
                "public_metrics": {
                  "followers_count": 100,
                  "following_count": 10,
                  "like_count": 5,
                  "listed_count": 1,
                  "media_count": 2,
                  "post_count": 500
                }
              }
            }
            """;
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(SuccessResponse(json)));
        var client = CreateClient(handler);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "2244994945" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Body?.Data);
        Assert.Equal("2244994945", response.Body!.Data!.Id);
        Assert.Equal("X Dev", response.Body.Data.Name);
        Assert.Equal("XDevelopers", response.Body.Data.Username);
        Assert.False(response.Body.Data.Protected);
        Assert.True(response.Body.Data.Verified);
        Assert.Equal(100, response.Body.Data.PublicMetrics?.FollowersCount);
    }

    [Fact]
    public async Task Preserves_unknown_response_fields_via_extension_data()
    {
        // SER-08: a new field X adds server-side must not break deserialization.
        const string json = """{"data":{"id":"1","name":"A","username":"a","brand_new_field_from_x":"surprise"}}""";
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(SuccessResponse(json)));
        var client = CreateClient(handler);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.True(response.Body!.Data!.ExtensionData.ContainsKey("brand_new_field_from_x"));
    }

    [Fact]
    public async Task Maps_a_documented_problem_error_to_XApiException()
    {
        const string problemJson = """
            {
              "type": "https://api.x.com/2/problems/resource-not-found",
              "title": "Not Found Error",
              "detail": "Could not find user with id: [999].",
              "status": 404,
              "resource_type": "user",
              "resource_id": "999"
            }
            """;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(problemJson, Encoding.UTF8, "application/problem+json"),
            };
            return Task.FromResult(response);
        });
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<XApiException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "999" }));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal("Not Found Error", ex.Problem?.Title);
        Assert.Equal("https://api.x.com/2/problems/resource-not-found", ex.Problem?.Type);
        Assert.True(ex.Problem!.ExtensionData.ContainsKey("resource_id"));
    }

    [Fact]
    public async Task Maps_401_to_XAuthenticationException()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<XAuthenticationException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));
    }

    [Fact]
    public async Task Maps_429_to_XRateLimitException_with_retry_after()
    {
        // MaxRetries: 0 - this checks the exception mapping in isolation from retry behavior.
        // Retry-then-give-up-within-budget for 429 is covered by RetryTests in UnitTests.
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        });
        var client = CreateClient(handler, options: new XClientOptions { MaxRetries = 0 });

        var ex = await Assert.ThrowsAsync<XRateLimitException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Equal(TimeSpan.FromSeconds(30), ex.RetryAfter);
    }

    [Fact]
    public async Task Empty_204_body_does_not_attempt_json_deserialization()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        var client = CreateClient(handler);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(response.Body);
    }

    private static HttpResponseMessage SuccessResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    private static XApiClient CreateClient(HttpMessageHandler handler, string token = "token", XClientOptions? options = null)
    {
        var httpClient = new HttpClient(handler);
        return new XApiClient(httpClient, new BearerTokenAuthenticationProvider(token), options);
    }
}

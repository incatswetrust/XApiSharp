using System.Net;
using System.Text;
using XApiSharp;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Pagination;
using XApiSharp.Users;

namespace XApiSharp.PackageTests;

/// <summary>
/// Spec section 22.3: read, error, pagination, and cancellation against the packed
/// <c>XApiSharp.Net</c> package - a public-API-only smoke test proving the shipped assembly
/// actually works standalone, not a substitute for the full unit/contract suite (which runs
/// against source, see tests/XApiSharp.UnitTests and tests/XApiSharp.ContractTests).
/// </summary>
public class ConsumerScenarioTests
{
    [Fact]
    public async Task Read_a_user_through_the_packed_client()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(
            HttpStatusCode.OK,
            """{"data":{"id":"2244994945","name":"X Developers","username":"XDevelopers"}}""")));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "2244994945" });

        Assert.Equal("XDevelopers", response.Body?.Data?.Username);
    }

    [Fact]
    public async Task An_error_response_throws_the_typed_exception_hierarchy()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(JsonResponse(
            HttpStatusCode.NotFound,
            """{"title":"Not Found Error","type":"https://api.x.com/2/problems/resource-not-found","status":404,"detail":"Could not find user."}""")));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        var ex = await Assert.ThrowsAsync<XApiException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "0" }));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
        Assert.Equal("Not Found Error", ex.Problem?.Title);
    }

    [Fact]
    public async Task Pagination_flattens_across_pages_using_the_shared_engine()
    {
        var pageCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            pageCount++;
            var body = request.RequestUri!.Query.Contains("pagination_token", StringComparison.Ordinal)
                ? """{"data":[{"id":"2","username":"b"}]}"""
                : """{"data":[{"id":"1","username":"a"}],"meta":{"next_token":"page2"}}""";
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, body));
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        var users = new List<string>();
        await foreach (var user in client.Users.GetFollowersAsync(new GetUsersPageRequest { UserId = "1" }))
        {
            users.Add(user.Username!);
        }

        Assert.Equal(["a", "b"], users);
        Assert.Equal(2, pageCount);
    }

    [Fact]
    public async Task An_already_cancelled_token_stops_the_call_without_reaching_the_network()
    {
        var networkCalled = false;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            networkCalled = true;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, "{}"));
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }, cancellationToken: cts.Token));

        Assert.False(networkCalled);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}

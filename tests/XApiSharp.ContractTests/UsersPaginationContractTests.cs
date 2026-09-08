using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Pagination;
using XApiSharp.Users;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Users family's paginated GET operations (spec section 19.2/19.3:
/// pagination-specific scenarios - empty intermediate page, MaxPages/MaxItems, forwarded filters).
/// <c>XPaginatorTests</c> (UnitTests project) already covers the pagination engine itself against
/// a fake page source; these confirm the Users family wires it up correctly end to end (real path/query
/// construction, the actual response shape). Full three-method (page/pages/items) coverage lives
/// on Followers as the representative operation; Following/Blocking/Muting/Affiliates - which
/// share the exact same request/response/pagination shape - get one lighter path+param check
/// each, and Search gets its own coverage for its different <c>next_token</c> parameter (PAGE-02).
/// </summary>
public class UsersPaginationContractTests
{
    [Fact]
    public async Task Followers_single_page_sends_max_results_and_pagination_token()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/followers", request.RequestUri!.AbsolutePath);
            Assert.Contains("max_results=10", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.Contains("pagination_token=abc", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"A","username":"a"}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetFollowersPageAsync(new GetUsersPageRequest
        {
            UserId = "1",
            MaxResults = 10,
            PaginationToken = "abc",
        });

        Assert.Single(response.Body!.Data!);
    }

    [Fact]
    public async Task Followers_pages_does_not_fetch_before_enumeration_and_stops_on_a_missing_next_token()
    {
        // PAGE-01/PAGE-05 wired end to end (not just at the XPaginator level).
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            var isFirstCall = !request.RequestUri!.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","name":"A","username":"a"}],"meta":{"next_token":"page2"}}"""
                : """{"data":[{"id":"2","name":"B","username":"b"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);
        _ = client.Users.GetFollowersPagesAsync(new GetUsersPageRequest { UserId = "1" });
        Assert.Equal(0, callCount);

        var users = new List<string>();
        await foreach (var user in client.Users.GetFollowersAsync(new GetUsersPageRequest { UserId = "1" }))
        {
            users.Add(user.Id);
        }

        Assert.Equal(["1", "2"], users);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Followers_items_honors_MaxItems_across_pages()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            callCount++;
            var body = $"{{\"data\":[{{\"id\":\"{callCount}a\",\"name\":\"A\",\"username\":\"a\"}}],\"meta\":{{\"next_token\":\"t{callCount}\"}}}}";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var users = new List<string>();
        await foreach (var user in client.Users.GetFollowersAsync(
            new GetUsersPageRequest { UserId = "1" }, new XPaginationOptions { MaxItems = 2 }))
        {
            users.Add(user.Id);
        }

        Assert.Equal(["1a", "2a"], users);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Following_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/following", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetFollowingPageAsync(new GetUsersPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task Blocking_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/blocking", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetBlockingPageAsync(new GetUsersPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task Muting_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/muting", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetMutingPageAsync(new GetUsersPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task Affiliates_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/affiliates", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        var client = CreateClient(handler);

        await client.Users.GetAffiliatesPageAsync(new GetUsersPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task Search_uses_next_token_not_pagination_token()
    {
        // PAGE-02: this operation's continuation parameter is genuinely named differently.
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/search", request.RequestUri!.AbsolutePath);
            Assert.Contains("query=ada", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.Contains("next_token=xyz", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.DoesNotContain("pagination_token", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"Ada","username":"ada"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.SearchPageAsync(new SearchUsersRequest { Query = "ada", NextToken = "xyz" });

        Assert.Single(response.Body!.Data!);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

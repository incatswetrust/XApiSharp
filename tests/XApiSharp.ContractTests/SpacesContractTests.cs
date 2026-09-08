using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Spaces;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the Spaces family (6 operations), per spec section 19.3. None of
/// the four lookup operations are paginated (no continuation token in the registry) - Buyers and
/// Posts are, and get full XPaginator-backed coverage.</summary>
public class SpacesContractTests
{
    [Fact]
    public async Task GetById_sends_the_full_field_selection_set()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/spaces/1", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("space.fields=title", query, StringComparison.Ordinal);
            Assert.Contains("expansions=creator_id", query, StringComparison.Ordinal);
            Assert.Contains("user.fields=username", query, StringComparison.Ordinal);
            Assert.Contains("topic.fields=name", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","title":"Chat"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Spaces.GetByIdAsync(new GetSpaceRequest
        {
            Id = "1",
            Fields = new SpaceFieldSelection
            {
                SpaceFields = [XSpaceField.Title],
                Expansions = [XExpansion.CreatorId],
                UserFields = [XUserField.Username],
                TopicFields = [XTopicField.Name],
            },
        });

        Assert.Equal("Chat", response.Body!.Data!.Title);
    }

    [Fact]
    public async Task GetByIds_sends_comma_joined_ids_with_no_meta_in_the_response()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/spaces", request.RequestUri!.AbsolutePath);
            Assert.Contains("ids=1%2C2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1"},{"id":"2"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Spaces.GetByIdsAsync(new GetSpacesByIdsRequest { Ids = ["1", "2"] });

        Assert.Equal(2, response.Body!.Data!.Count);
        Assert.Null(response.Body.Meta);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Spaces.GetByIdsAsync(new GetSpacesByIdsRequest { Ids = [] }));
    }

    [Fact]
    public async Task GetByCreatorIds_sends_comma_joined_user_ids_and_deserializes_result_count()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/spaces/by/creator_ids", request.RequestUri!.AbsolutePath);
            Assert.Contains("user_ids=9", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1"}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Spaces.GetByCreatorIdsAsync(new GetSpacesByCreatorIdsRequest { UserIds = ["9"] });

        Assert.Equal(1, response.Body!.Meta!.ResultCount);
    }

    [Fact]
    public async Task Search_sends_query_and_state()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/spaces/search", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("query=xapi", query, StringComparison.Ordinal);
            Assert.Contains("state=live", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        var client = CreateClient(handler);

        await client.Spaces.SearchAsync(new SearchSpacesRequest { Query = "xapi", State = XSpaceState.Live });
    }

    [Fact]
    public async Task Buyers_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/spaces/1/buyers", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","name":"A","username":"a"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","name":"B","username":"b"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var user in client.Spaces.GetBuyersAsync(new SpaceBuyersPageRequest { SpaceId = "1" }))
        {
            ids.Add(user.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Posts_sends_correct_path_and_deserializes_items()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/spaces/1/tweets", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","text":"hi"}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Spaces.GetPostsPageAsync(new SpacePostsPageRequest { SpaceId = "1" });

        Assert.Equal("hi", response.Body!.Data![0].Text);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

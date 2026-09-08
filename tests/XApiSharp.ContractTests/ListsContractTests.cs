using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Lists;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the Lists family (9 operations), per spec section 19.3.</summary>
public class ListsContractTests
{
    [Fact]
    public async Task Create_sends_name_and_private_and_returns_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/lists", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Friends", doc.RootElement.GetProperty("name").GetString());
            Assert.True(doc.RootElement.GetProperty("private").GetBoolean());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"1","name":"Friends"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var response = await client.Lists.CreateAsync(new CreateListRequest { Name = "Friends", Private = true });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("1", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task Delete_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/lists/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Lists.DeleteAsync(new DeleteListRequest { Id = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task GetById_sends_list_fields_expansions_and_user_fields_and_deserializes_owner()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/lists/1", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("list.fields=member_count", query, StringComparison.Ordinal);
            Assert.Contains("expansions=owner_id", query, StringComparison.Ordinal);
            Assert.Contains("user.fields=username", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse(
                """{"data":{"id":"1","name":"L","owner_id":"9"},"includes":{"users":[{"id":"9","name":"O","username":"owner"}]}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Lists.GetByIdAsync(new GetListRequest
        {
            Id = "1",
            Fields = [XListField.MemberCount],
            Expansions = [XExpansion.OwnerId],
            UserFields = [XUserField.Username],
        });

        Assert.Equal("owner", response.Body!.Includes!.Users![0].Username);
    }

    [Fact]
    public async Task Update_sends_only_the_fields_that_were_set()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/2/lists/1", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("New Name", doc.RootElement.GetProperty("name").GetString());
            Assert.False(doc.RootElement.TryGetProperty("private", out _));
            return SuccessResponse("""{"data":{"updated":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Lists.UpdateAsync(new UpdateListRequest { Id = "1", Name = "New Name" });

        Assert.True(response.Body!.Data!.Updated);
    }

    [Fact]
    public async Task Followers_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/lists/1/followers", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","name":"A","username":"a"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","name":"B","username":"b"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var user in client.Lists.GetFollowersAsync(new ListUsersPageRequest { ListId = "1" }))
        {
            ids.Add(user.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Members_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/lists/1/members", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Lists.GetMembersPageAsync(new ListUsersPageRequest { ListId = "1" });
    }

    [Fact]
    public async Task AddMember_sends_user_id_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/lists/1/members", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("9", doc.RootElement.GetProperty("user_id").GetString());
            return SuccessResponse("""{"data":{"is_member":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Lists.AddMemberAsync(new AddListMemberRequest { ListId = "1", UserId = "9" });

        Assert.True(response.Body!.Data!.IsMember);
    }

    [Fact]
    public async Task RemoveMember_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/lists/1/members/9", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"is_member":false}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Lists.RemoveMemberAsync(new RemoveListMemberRequest { ListId = "1", UserId = "9" });

        Assert.False(response.Body!.Data!.IsMember);
    }

    [Fact]
    public async Task Posts_sends_correct_path_and_deserializes_items()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/lists/1/tweets", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","text":"hi"}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Lists.GetPostsPageAsync(new ListPostsPageRequest { ListId = "1" });

        Assert.Equal("hi", response.Body!.Data![0].Text);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

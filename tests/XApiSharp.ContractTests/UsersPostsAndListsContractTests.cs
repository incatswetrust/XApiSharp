using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Users;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Users family's remaining paginated/list GET operations: Posts/
/// Mentions/Timeline/LikedPosts/Bookmarks/RepostsOfMe (page over <see cref="Post"/>) and
/// FollowedLists/ListMemberships/OwnedLists/PinnedLists (page over <see cref="XList"/>), plus the
/// two bookmark-folder operations that accept pagination-looking parameters but document no
/// `meta` (single-page only). Full coverage lives on Posts (representative Post-list operation)
/// and FollowedLists (representative List-list operation); the rest get one path+param check each
/// per spec 19.3's "don't duplicate the same check hundreds of times without added value".
/// </summary>
public class UsersPostsAndListsContractTests
{
    [Fact]
    public async Task Posts_sends_all_documented_query_parameters()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/tweets", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("max_results=10", query, StringComparison.Ordinal);
            Assert.Contains("since_id=100", query, StringComparison.Ordinal);
            Assert.Contains("until_id=200", query, StringComparison.Ordinal);
            Assert.Contains("exclude=replies%2Cretweets", query, StringComparison.Ordinal);
            Assert.Contains("post.fields=text", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","text":"hi"}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetPostsPageAsync(new GetPostsPageRequest
        {
            UserId = "1",
            MaxResults = 10,
            SinceId = "100",
            UntilId = "200",
            Exclude = [XPostExclusionFilter.Replies, XPostExclusionFilter.Retweets],
            Fields = [XPostField.Text],
        });

        Assert.Equal("hi", response.Body!.Data![0].Text);
    }

    [Fact]
    public async Task Posts_items_traverses_pages_lazily_via_pagination_token()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            var isFirstCall = !request.RequestUri!.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","text":"a"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","text":"b"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var post in client.Users.GetPostsAsync(new GetPostsPageRequest { UserId = "1" }))
        {
            ids.Add(post.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Mentions_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/mentions", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetMentionsPageAsync(new GetPostsPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task Timeline_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/timelines/reverse_chronological", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetTimelinePageAsync(new GetPostsPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task LikedPosts_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/liked_tweets", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetLikedPostsPageAsync(new GetPostsPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task Bookmarks_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/bookmarks", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetBookmarksPageAsync(new GetPostsPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task RepostsOfMe_has_no_id_path_segment_and_accepts_a_null_request()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/reposts_of_me", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetRepostsOfMePageAsync();
    }

    [Fact]
    public async Task FollowedLists_sends_list_fields_expansions_and_user_fields_and_deserializes_owner()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/followed_lists", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("list.fields=name", query, StringComparison.Ordinal);
            Assert.Contains("expansions=owner_id", query, StringComparison.Ordinal);
            Assert.Contains("user.fields=username", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse(
                """{"data":[{"id":"1","name":"My List","owner_id":"9"}],"includes":{"users":[{"id":"9","name":"Owner","username":"owner"}]},"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetFollowedListsPageAsync(new GetListsPageRequest
        {
            UserId = "1",
            Fields = [XListField.Name],
            Expansions = [XExpansion.OwnerId],
            UserFields = [XUserField.Username],
        });

        Assert.Equal("My List", response.Body!.Data![0].Name);
        Assert.Equal("owner", response.Body.Includes!.Users![0].Username);
    }

    [Fact]
    public async Task FollowedLists_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            var isFirstCall = !request.RequestUri!.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","name":"A"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","name":"B"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var list in client.Users.GetFollowedListsAsync(new GetListsPageRequest { UserId = "1" }))
        {
            ids.Add(list.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ListMemberships_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/list_memberships", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetListMembershipsPageAsync(new GetListsPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task OwnedLists_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/owned_lists", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Users.GetOwnedListsPageAsync(new GetListsPageRequest { UserId = "1" });
    }

    [Fact]
    public async Task PinnedLists_sends_no_pagination_parameters()
    {
        // Unlike the other three Lists operations, the registry exposes no
        // max_results/pagination_token here at all.
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/pinned_lists", request.RequestUri!.AbsolutePath);
            Assert.DoesNotContain("pagination_token", request.RequestUri.Query, StringComparison.Ordinal);
            Assert.DoesNotContain("max_results", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"Pinned"}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetPinnedListsAsync(new GetPinnedListsRequest { UserId = "1" });

        Assert.Equal("Pinned", response.Body!.Data![0].Name);
    }

    [Fact]
    public async Task BookmarkFolders_deserializes_folder_list()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/bookmarks/folders", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"Reading"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetBookmarkFoldersAsync(new GetBookmarkFoldersRequest { UserId = "1" });

        Assert.Equal("Reading", response.Body!.Data![0].Name);
    }

    [Fact]
    public async Task BookmarksByFolder_substitutes_both_path_segments_and_deserializes_ids_only()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/bookmarks/folders/7", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"555"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.GetBookmarksByFolderAsync(new GetBookmarksByFolderRequest { UserId = "1", FolderId = "7" });

        Assert.Equal("555", response.Body!.Data![0].Id);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

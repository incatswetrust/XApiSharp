using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Users;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Users family's boolean-toggle relationship operations (follow/mute/
/// like/repost/dm-block, list follow/pin, bookmarks) per spec section 19.3. Generic HTTP
/// status/auth-error mapping is covered once already (<see cref="UsersGetByIdContractTests"/>)
/// and is not repeated per operation - each check here verifies method/path/body construction and
/// documented success-body deserialization, which is genuinely per-operation.
/// </summary>
public class UsersActionsContractTests
{
    [Fact]
    public async Task Follow_sends_target_user_id_body_and_deserializes_pending_follow()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/users/1/following", request.RequestUri!.AbsolutePath);
            Assert.Equal("2", await ReadJsonField(request, "target_user_id"));
            return SuccessResponse("""{"data":{"following":false,"pending_follow":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Users.FollowAsync(new FollowUserRequest { SourceUserId = "1", TargetUserId = "2" });

        Assert.True(response.Body!.Data!.PendingFollow);
        Assert.False(response.Body.Data.Following);
    }

    [Fact]
    public async Task Unfollow_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/users/1/following/2", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"following":false}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.UnfollowAsync(new UnfollowUserRequest { SourceUserId = "1", TargetUserId = "2" });

        Assert.False(response.Body!.Data!.Following);
    }

    [Fact]
    public async Task Mute_sends_target_user_id_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("/2/users/1/muting", request.RequestUri!.AbsolutePath);
            Assert.Equal("2", await ReadJsonField(request, "target_user_id"));
            return SuccessResponse("""{"data":{"muting":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Users.MuteAsync(new MuteUserRequest { SourceUserId = "1", TargetUserId = "2" });

        Assert.True(response.Body!.Data!.Muting);
    }

    [Fact]
    public async Task Unmute_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/muting/2", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"muting":false}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.UnmuteAsync(new UnmuteUserRequest { SourceUserId = "1", TargetUserId = "2" });
    }

    [Fact]
    public async Task Like_sends_tweet_id_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("/2/users/1/likes", request.RequestUri!.AbsolutePath);
            Assert.Equal("9", await ReadJsonField(request, "tweet_id"));
            return SuccessResponse("""{"data":{"liked":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Users.LikeAsync(new LikePostRequest { UserId = "1", TweetId = "9" });

        Assert.True(response.Body!.Data!.Liked);
    }

    [Fact]
    public async Task Unlike_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/likes/9", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"liked":false}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.UnlikeAsync(new UnlikePostRequest { UserId = "1", TweetId = "9" });
    }

    [Fact]
    public async Task Repost_sends_tweet_id_body_and_deserializes_rest_id()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("/2/users/1/retweets", request.RequestUri!.AbsolutePath);
            Assert.Equal("9", await ReadJsonField(request, "tweet_id"));
            return SuccessResponse("""{"data":{"retweeted":true,"rest_id":"555"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Users.RepostAsync(new RepostPostRequest { UserId = "1", TweetId = "9" });

        Assert.Equal("555", response.Body!.Data!.RestId);
    }

    [Fact]
    public async Task Unrepost_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/retweets/9", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"retweeted":false}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.UnrepostAsync(new UnrepostPostRequest { UserId = "1", SourceTweetId = "9" });
    }

    [Fact]
    public async Task BlockDms_sends_no_body()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/users/2/dm/block", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Content);
            return Task.FromResult(SuccessResponse("""{"data":{"blocked":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.BlockDmsAsync(new BlockUserDmsRequest { TargetUserId = "2" });

        Assert.True(response.Body!.Data!.Blocked);
    }

    [Fact]
    public async Task UnblockDms_sends_no_body()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/2/dm/unblock", request.RequestUri!.AbsolutePath);
            Assert.Null(request.Content);
            return Task.FromResult(SuccessResponse("""{"data":{"blocked":false}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.UnblockDmsAsync(new UnblockUserDmsRequest { TargetUserId = "2" });
    }

    [Fact]
    public async Task FollowList_sends_list_id_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("/2/users/1/followed_lists", request.RequestUri!.AbsolutePath);
            Assert.Equal("42", await ReadJsonField(request, "list_id"));
            return SuccessResponse("""{"data":{"following":true}}""");
        });
        var client = CreateClient(handler);

        await client.Users.FollowListAsync(new FollowListRequest { UserId = "1", ListId = "42" });
    }

    [Fact]
    public async Task UnfollowList_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/followed_lists/42", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"following":false}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.UnfollowListAsync(new UnfollowListRequest { UserId = "1", ListId = "42" });
    }

    [Fact]
    public async Task PinList_sends_list_id_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("/2/users/1/pinned_lists", request.RequestUri!.AbsolutePath);
            Assert.Equal("42", await ReadJsonField(request, "list_id"));
            return SuccessResponse("""{"data":{"pinned":true}}""");
        });
        var client = CreateClient(handler);

        await client.Users.PinListAsync(new PinListRequest { UserId = "1", ListId = "42" });
    }

    [Fact]
    public async Task UnpinList_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/pinned_lists/42", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"pinned":false}}"""));
        });
        var client = CreateClient(handler);

        await client.Users.UnpinListAsync(new UnpinListRequest { UserId = "1", ListId = "42" });
    }

    [Fact]
    public async Task CreateBookmark_single_omits_folder_id_when_not_set()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/users/1/bookmarks", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            Assert.DoesNotContain("folder_id", json, StringComparison.Ordinal);
            return SuccessResponse("""{"data":{"bookmarked":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Users.CreateBookmarkAsync(new CreateBookmarkRequest { UserId = "1", TweetId = "9" });

        Assert.True(response.Body!.Data!.Bookmarked);
    }

    [Fact]
    public async Task CreateBookmark_single_sends_folder_id_when_set()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("7", await ReadJsonField(request, "folder_id"));
            return SuccessResponse("""{"data":{"bookmarked":true}}""");
        });
        var client = CreateClient(handler);

        await client.Users.CreateBookmarkAsync(new CreateBookmarkRequest { UserId = "1", TweetId = "9", FolderId = "7" });
    }

    [Fact]
    public async Task CreateBookmarks_batch_deserializes_per_id_results_and_errors_without_dropping_partial_success()
    {
        // Spec 12.1: a batch call's per-ID failures must survive alongside the IDs that succeeded.
        const string json = """
            {
              "data": [{"tweet_id": "1", "bookmarked": true}],
              "errors": [{"tweet_id": "2", "title": "Not Found", "detail": "no such post"}]
            }
            """;
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal(["1", "2"], await ReadJsonArrayField(request, "tweet_ids"));
            return SuccessResponse(json);
        });
        var client = CreateClient(handler);

        var response = await client.Users.CreateBookmarksAsync(new CreateBookmarksRequest { UserId = "1", TweetIds = ["1", "2"] });

        Assert.True(response.HasErrors);
        Assert.True(response.IsPartialSuccess);
        Assert.Single(response.Body!.Data!);
        Assert.Equal("2", response.Body.Errors![0].TweetId);
    }

    [Fact]
    public async Task CreateBookmarks_requires_at_least_one_id()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
            throw new InvalidOperationException("should not send a request"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Users.CreateBookmarksAsync(new CreateBookmarksRequest { UserId = "1", TweetIds = [] }));
    }

    [Fact]
    public async Task DeleteBookmark_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/users/1/bookmarks/9", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"bookmarked":false}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Users.DeleteBookmarkAsync(new DeleteBookmarkRequest { UserId = "1", TweetId = "9" });

        Assert.False(response.Body!.Data!.Bookmarked);
    }

    [Fact]
    public async Task CreateBookmarkFolder_sends_name_body_and_accepts_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, _) =>
        {
            Assert.Equal("/2/users/1/bookmarks/folders", request.RequestUri!.AbsolutePath);
            Assert.Equal("Reading list", await ReadJsonField(request, "name"));
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"1","name":"Reading list"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var response = await client.Users.CreateBookmarkFolderAsync(new CreateBookmarkFolderRequest { UserId = "1", Name = "Reading list" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("1", response.Body!.Data!.Id);
    }

    private static async Task<string?> ReadJsonField(HttpRequestMessage request, string field)
    {
        var json = await request.Content!.ReadAsStringAsync(CancellationToken.None);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty(field).GetString();
    }

    private static async Task<List<string>> ReadJsonArrayField(HttpRequestMessage request, string field)
    {
        var json = await request.Content!.ReadAsStringAsync(CancellationToken.None);
        using var doc = JsonDocument.Parse(json);
        return [.. doc.RootElement.GetProperty(field).EnumerateArray().Select(e => e.GetString()!)];
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

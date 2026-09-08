using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Posts;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Posts family (14 operations), per spec section 19.3. Full coverage on
/// GetById/GetByIds/Create/Delete/HideReply plus the pagination engine wiring (LikingUsers as the
/// representative paginated operation); the remaining paginated sibling operations
/// (RepostedBy/QuotePosts/Reposts/search/counts) get one path+param check each - they share the
/// exact same request/response types and XPaginator wiring already covered by
/// <see cref="UsersPaginationContractTests"/> and the LikingUsers test here.
/// </summary>
public class PostsContractTests
{
    [Fact]
    public async Task GetById_sends_the_full_field_selection_set()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/1", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("post.fields=text", query, StringComparison.Ordinal);
            Assert.Contains("expansions=author_id", query, StringComparison.Ordinal);
            Assert.Contains("user.fields=username", query, StringComparison.Ordinal);
            Assert.Contains("media.fields=url", query, StringComparison.Ordinal);
            Assert.Contains("poll.fields=options", query, StringComparison.Ordinal);
            Assert.Contains("place.fields=name", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","text":"hi"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Posts.GetByIdAsync(new GetPostRequest
        {
            Id = "1",
            Fields = new XPostFieldSelection
            {
                PostFields = [XPostField.Text],
                Expansions = [XExpansion.AuthorId],
                UserFields = [XUserField.Username],
                MediaFields = [XMediaField.Url],
                PollFields = [XPollField.Options],
                PlaceFields = [XPlaceField.Name],
            },
        });

        Assert.Equal("hi", response.Body!.Data!.Text);
    }

    [Fact]
    public async Task GetByIds_sends_comma_joined_ids_and_requires_at_least_one()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets", request.RequestUri!.AbsolutePath);
            Assert.Contains("ids=1%2C2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","text":"a"},{"id":"2","text":"b"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Posts.GetByIdsAsync(new GetPostsByIdsRequest { Ids = ["1", "2"] });

        Assert.Equal(2, response.Body!.Data!.Count);

        await Assert.ThrowsAsync<ArgumentException>(
            () => client.Posts.GetByIdsAsync(new GetPostsByIdsRequest { Ids = [] }));
    }

    [Fact]
    public async Task Create_sends_text_and_reply_and_returns_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/tweets", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("hello world", doc.RootElement.GetProperty("text").GetString());
            Assert.Equal("42", doc.RootElement.GetProperty("reply").GetProperty("in_reply_to_tweet_id").GetString());
            Assert.False(doc.RootElement.TryGetProperty("media", out _));
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"999","text":"hello world"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var response = await client.Posts.CreateAsync(new CreatePostRequest
        {
            Text = "hello world",
            Reply = new CreatePostReply { InReplyToTweetId = "42" },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("999", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task Create_defaults_text_to_empty_string_when_only_media_is_set()
    {
        // The registry's own note: the backend rejects an absent `text`, even for a media-only
        // Post - it must always be sent, defaulted to "".
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("", doc.RootElement.GetProperty("text").GetString());
            Assert.Equal(1, doc.RootElement.GetProperty("media").GetProperty("media_ids").GetArrayLength());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"1","text":""}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        await client.Posts.CreateAsync(new CreatePostRequest { Media = new CreatePostMedia { MediaIds = ["777"] } });
    }

    [Fact]
    public async Task Delete_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/tweets/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Posts.DeleteAsync(new DeletePostRequest { Id = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task HideReply_sends_hidden_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/2/tweets/1/hidden", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.GetProperty("hidden").GetBoolean());
            return SuccessResponse("""{"data":{"hidden":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Posts.HideReplyAsync(new HideReplyRequest { TweetId = "1", Hidden = true });

        Assert.True(response.Body!.Data!.Hidden);
    }

    [Fact]
    public async Task LikingUsers_items_traverses_pages_lazily_via_pagination_token()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/tweets/1/liking_users", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","name":"A","username":"a"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","name":"B","username":"b"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var user in client.Posts.GetLikingUsersAsync(new PostUsersPageRequest { PostId = "1" }))
        {
            ids.Add(user.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task RepostedBy_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/1/retweeted_by", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Posts.GetRepostedByPageAsync(new PostUsersPageRequest { PostId = "1" });
    }

    [Fact]
    public async Task QuotePosts_sends_exclude_filter()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/1/quote_tweets", request.RequestUri!.AbsolutePath);
            Assert.Contains("exclude=replies", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        var client = CreateClient(handler);

        await client.Posts.GetQuotePostsPageAsync(new PostsPageRequest { PostId = "1", Exclude = [XPostExclusionFilter.Replies] });
    }

    [Fact]
    public async Task Reposts_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/1/retweets", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Posts.GetRepostsPageAsync(new PostsPageRequest { PostId = "1" });
    }

    [Fact]
    public async Task SearchRecent_sends_query_and_sort_order()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/search/recent", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("query=from%3Aada", query, StringComparison.Ordinal);
            Assert.Contains("sort_order=recency", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","text":"hi"}],"meta":{"newest_id":"1","oldest_id":"1","result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Posts.SearchRecentPageAsync(new SearchPostsRequest { Query = "from:ada", SortOrder = XSortOrder.Recency });

        Assert.Equal("1", response.Body!.Meta!.NewestId);
    }

    [Fact]
    public async Task SearchAll_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/search/all", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Posts.SearchAllPageAsync(new SearchPostsRequest { Query = "x" });
    }

    [Fact]
    public async Task CountsRecent_deserializes_buckets_and_total()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/counts/recent", request.RequestUri!.AbsolutePath);
            Assert.Contains("query=xapi", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse(
                """{"data":[{"start":"2024-01-01T00:00:00Z","end":"2024-01-02T00:00:00Z","post_count":5}],"meta":{"total_post_count":5}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Posts.GetCountsRecentPageAsync(new GetPostCountsRequest { Query = "xapi" });

        Assert.Equal(5, response.Body!.Data![0].PostCount);
        Assert.Equal(5, response.Body.Meta!.TotalPostCount);
    }

    [Fact]
    public async Task CountsAll_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/counts/all", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).Posts.GetCountsAllPageAsync(new GetPostCountsRequest { Query = "x" });
    }

    [Fact]
    public async Task Analytics_sends_ids_time_range_and_granularity_and_requires_at_least_one_id()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/analytics", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("ids=1", query, StringComparison.Ordinal);
            Assert.Contains("granularity=total", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","likes":10}]}"""));
        });
        var client = CreateClient(handler);
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);

        var response = await client.Posts.GetAnalyticsAsync(new GetAnalyticsRequest
        {
            Ids = ["1"],
            StartTime = start,
            EndTime = end,
            Granularity = XAnalyticsGranularity.Total,
        });

        Assert.Equal(10, response.Body!.Data![0].Likes);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Posts.GetAnalyticsAsync(new GetAnalyticsRequest
        {
            Ids = [],
            StartTime = start,
            EndTime = end,
        }));
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

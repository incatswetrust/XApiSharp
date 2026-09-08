using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.Posts;

/// <summary>Typed methods for the Posts family (14 operations per the registry).</summary>
public sealed class PostsClient
{
    private readonly RequestExecutor _executor;

    internal PostsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/tweets/{id}</c> - Get Posts by ID. Requires app-only bearer, OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetPostResponse>> GetByIdAsync(GetPostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<GetPostResponse>(
            HttpMethod.Get,
            $"2/tweets/{Uri.EscapeDataString(request.Id)}",
            PostFieldSelectionQuery(request.Fields),
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/tweets</c> - Get Posts by IDs (up to 100 per call). Requires app-only bearer,
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetPostsByIdsResponse>> GetByIdsAsync(GetPostsByIdsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ids.Count == 0)
        {
            throw new ArgumentException("At least one ID is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
        };
        query.AddRange(PostFieldSelectionQuery(request.Fields));

        return _executor.SendAsync<GetPostsByIdsResponse>(HttpMethod.Get, "2/tweets", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/tweets</c> - Create Posts. Requires OAuth 2.0
    /// (<c>tweet.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a. Returns HTTP
    /// 201.
    /// </summary>
    public Task<XResponse<CreatePostResponse>> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = new CreatePostBody
        {
            Text = request.Text ?? "",
            Reply = request.Reply is { } reply
                ? new CreatePostReplyBody { InReplyToTweetId = reply.InReplyToTweetId, ExcludeReplyUserIds = reply.ExcludeReplyUserIds }
                : null,
            EditOptions = request.EditOptions is { } edit
                ? new CreatePostEditOptionsBody { PreviousPostId = edit.PreviousPostId }
                : null,
            QuoteTweetId = request.QuoteTweetId,
            Media = request.Media is { } media
                ? new CreatePostMediaBody
                {
                    MediaIds = media.MediaIds,
                    TaggedUserIds = media.TaggedUserIds,
                    PreviewMediaId = media.PreviewMediaId,
                    Description = media.Description,
                    Title = media.Title,
                    Embeddable = media.Embeddable,
                    CallToActions = media.CallToActions,
                }
                : null,
            Poll = request.Poll is { } poll
                ? new CreatePostPollBody { Options = poll.Options, DurationMinutes = poll.DurationMinutes }
                : null,
            Geo = request.Geo is { } geo ? new CreatePostGeoBody { PlaceId = geo.PlaceId } : null,
            ReplySettings = request.ReplySettings?.ToApiValue(),
            CommunityId = request.CommunityId,
            CardUri = request.CardUri,
            DirectMessageDeepLink = request.DirectMessageDeepLink,
            ForSuperFollowersOnly = request.ForSuperFollowersOnly,
            MadeWithAi = request.MadeWithAi,
            Nullcast = request.Nullcast,
            PaidPartnership = request.PaidPartnership,
            ShareWithFollowers = request.ShareWithFollowers,
        };

        return _executor.SendAsync<CreatePostResponse>(HttpMethod.Post, "2/tweets", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/tweets/{id}</c> - Delete Posts. Requires OAuth 2.0
    /// (<c>tweet.write</c> + <c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<DeletePostResponse>> DeleteAsync(DeletePostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<DeletePostResponse>(
            HttpMethod.Delete,
            $"2/tweets/{Uri.EscapeDataString(request.Id)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>PUT /2/tweets/{tweet_id}/hidden</c> - Hide reply. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c> + <c>tweet.moderate.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<HideReplyResponse>> HideReplyAsync(HideReplyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TweetId);

        return _executor.SendAsync<HideReplyResponse>(
            HttpMethod.Put,
            $"2/tweets/{Uri.EscapeDataString(request.TweetId)}/hidden",
            new HideReplyBody { Hidden = request.Hidden },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/liking_users</c> - one page. Requires OAuth 2.0
    /// (<c>like.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<PostUsersPageResponse>> GetLikingUsersPageAsync(PostUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return FetchPostUsersPageAsync(LikingUsersPath(request.PostId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/liking_users</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<PostUsersPageResponse>> GetLikingUsersPagesAsync(PostUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePostUsersPages(LikingUsersPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/liking_users</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetLikingUsersAsync(PostUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePostUsers(LikingUsersPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/retweeted_by</c> - one page. Requires app-only bearer,
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<PostUsersPageResponse>> GetRepostedByPageAsync(PostUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return FetchPostUsersPageAsync(RepostedByPath(request.PostId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/retweeted_by</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<PostUsersPageResponse>> GetRepostedByPagesAsync(PostUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePostUsersPages(RepostedByPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/retweeted_by</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetRepostedByAsync(PostUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePostUsers(RepostedByPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/quote_tweets</c> - one page. Requires app-only bearer,
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<PostsPageResponse>> GetQuotePostsPageAsync(PostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return FetchPostsPageAsync(QuotePostsPath(request.PostId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/quote_tweets</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<PostsPageResponse>> GetQuotePostsPagesAsync(PostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePostsPages(QuotePostsPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/quote_tweets</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetQuotePostsAsync(PostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePosts(QuotePostsPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/retweets</c> - one page. Requires app-only bearer, OAuth
    /// 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<PostsPageResponse>> GetRepostsPageAsync(PostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return FetchPostsPageAsync(RepostsPath(request.PostId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/retweets</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<PostsPageResponse>> GetRepostsPagesAsync(PostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePostsPages(RepostsPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/{id}/retweets</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetRepostsAsync(PostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);

        return EnumeratePosts(RepostsPath(request.PostId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/recent</c> - one page. Requires app-only bearer, OAuth
    /// 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<SearchPostsResponse>> SearchRecentPageAsync(SearchPostsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return FetchSearchPageAsync("2/tweets/search/recent", request, request.NextToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/recent</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SearchPostsResponse>> SearchRecentPagesAsync(SearchPostsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateSearchPages("2/tweets/search/recent", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/recent</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> SearchRecentAsync(SearchPostsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateSearch("2/tweets/search/recent", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/all</c> - one page. Requires OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a; academic/enterprise access level per
    /// X's own docs (not enforced client-side).</summary>
    public Task<XResponse<SearchPostsResponse>> SearchAllPageAsync(SearchPostsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return FetchSearchPageAsync("2/tweets/search/all", request, request.NextToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/all</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SearchPostsResponse>> SearchAllPagesAsync(SearchPostsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateSearchPages("2/tweets/search/all", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/all</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> SearchAllAsync(SearchPostsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateSearch("2/tweets/search/all", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/counts/recent</c> - one page. Requires app-only bearer or
    /// OAuth 2.0 (no documented scopes beyond authentication).</summary>
    public Task<XResponse<GetPostCountsResponse>> GetCountsRecentPageAsync(GetPostCountsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return FetchCountsPageAsync("2/tweets/counts/recent", request, request.NextToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/counts/recent</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostCountsResponse>> GetCountsRecentPagesAsync(GetPostCountsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateCountsPages("2/tweets/counts/recent", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/counts/recent</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<PostCountBucket> GetCountsRecentAsync(GetPostCountsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateCounts("2/tweets/counts/recent", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/counts/all</c> - one page. Requires app-only bearer.</summary>
    public Task<XResponse<GetPostCountsResponse>> GetCountsAllPageAsync(GetPostCountsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return FetchCountsPageAsync("2/tweets/counts/all", request, request.NextToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/counts/all</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostCountsResponse>> GetCountsAllPagesAsync(GetPostCountsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateCountsPages("2/tweets/counts/all", request, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/counts/all</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<PostCountBucket> GetCountsAllAsync(GetPostCountsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return EnumerateCounts("2/tweets/counts/all", request, options, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/tweets/analytics</c> - Get Posts Analytics (up to 100 IDs per call). Requires
    /// OAuth 2.0 (<c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a. Not paginated - the
    /// registry declares no <c>meta</c>/continuation token.
    /// </summary>
    public Task<XResponse<GetAnalyticsResponse>> GetAnalyticsAsync(GetAnalyticsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ids.Count == 0)
        {
            throw new ArgumentException("At least one ID is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
            ("start_time", request.StartTime.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime.ToString("O", CultureInfo.InvariantCulture)),
            ("granularity", request.Granularity?.ToApiValue()),
            ("analytics.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetAnalyticsResponse>(HttpMethod.Get, "2/tweets/analytics", query, cancellationToken);
    }

    private static string LikingUsersPath(string postId) => $"2/tweets/{Uri.EscapeDataString(postId)}/liking_users";

    private static string RepostedByPath(string postId) => $"2/tweets/{Uri.EscapeDataString(postId)}/retweeted_by";

    private static string QuotePostsPath(string postId) => $"2/tweets/{Uri.EscapeDataString(postId)}/quote_tweets";

    private static string RepostsPath(string postId) => $"2/tweets/{Uri.EscapeDataString(postId)}/retweets";

    private IAsyncEnumerable<XResponse<PostUsersPageResponse>> EnumeratePostUsersPages(string path, PostUsersPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<PostUsersPageResponse>(
            (token, ct) => FetchPostUsersPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<User> EnumeratePostUsers(string path, PostUsersPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<PostUsersPageResponse, User>(
            (token, ct) => FetchPostUsersPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<PostUsersPageResponse>> FetchPostUsersPageAsync(string path, PostUsersPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
        };

        return _executor.SendAsync<PostUsersPageResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    private IAsyncEnumerable<XResponse<PostsPageResponse>> EnumeratePostsPages(string path, PostsPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<PostsPageResponse>(
            (token, ct) => FetchPostsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<Post> EnumeratePosts(string path, PostsPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<PostsPageResponse, Post>(
            (token, ct) => FetchPostsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<PostsPageResponse>> FetchPostsPageAsync(string path, PostsPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("exclude", QueryStringBuilder.JoinCommaSeparated(request.Exclude, e => e.ToApiValue())),
        };
        query.AddRange(PostFieldSelectionQuery(request.Fields));

        return _executor.SendAsync<PostsPageResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    private IAsyncEnumerable<XResponse<SearchPostsResponse>> EnumerateSearchPages(string path, SearchPostsRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<SearchPostsResponse>(
            (token, ct) => FetchSearchPageAsync(path, request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<Post> EnumerateSearch(string path, SearchPostsRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<SearchPostsResponse, Post>(
            (token, ct) => FetchSearchPageAsync(path, request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<SearchPostsResponse>> FetchSearchPageAsync(string path, SearchPostsRequest request, string? nextToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("query", request.Query),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("next_token", nextToken),
            ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("since_id", request.SinceId),
            ("until_id", request.UntilId),
            ("sort_order", request.SortOrder?.ToApiValue()),
        };
        query.AddRange(PostFieldSelectionQuery(request.Fields));

        return _executor.SendAsync<SearchPostsResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    private IAsyncEnumerable<XResponse<GetPostCountsResponse>> EnumerateCountsPages(string path, GetPostCountsRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<GetPostCountsResponse>(
            (token, ct) => FetchCountsPageAsync(path, request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<PostCountBucket> EnumerateCounts(string path, GetPostCountsRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<GetPostCountsResponse, PostCountBucket>(
            (token, ct) => FetchCountsPageAsync(path, request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<GetPostCountsResponse>> FetchCountsPageAsync(string path, GetPostCountsRequest request, string? nextToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("query", request.Query),
            ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("since_id", request.SinceId),
            ("until_id", request.UntilId),
            ("next_token", nextToken),
            ("granularity", request.Granularity?.ToApiValue()),
        };

        return _executor.SendAsync<GetPostCountsResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    /// <summary>Builds the full <c>post.fields</c>/<c>expansions</c>/<c>user.fields</c>/
    /// <c>media.fields</c>/<c>poll.fields</c>/<c>place.fields</c> query parameter set
    /// (<see cref="XPostFieldSelection"/>, SER-05).</summary>
    private static IReadOnlyList<(string Name, string? Value)> PostFieldSelectionQuery(XPostFieldSelection? fields) =>
    [
        ("post.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PostFields, f => f.ToApiValue())),
        ("expansions", QueryStringBuilder.JoinCommaSeparated(fields?.Expansions, e => e.ToApiValue())),
        ("user.fields", QueryStringBuilder.JoinCommaSeparated(fields?.UserFields, f => f.ToApiValue())),
        ("media.fields", QueryStringBuilder.JoinCommaSeparated(fields?.MediaFields, f => f.ToApiValue())),
        ("poll.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PollFields, f => f.ToApiValue())),
        ("place.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PlaceFields, f => f.ToApiValue())),
    ];
}

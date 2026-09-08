using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.Users;

/// <summary>Typed methods for the Users family (36 operations per the registry; 2 -
/// <c>GET /2/users/public_keys</c> and <c>GET /2/users/{id}/public_keys</c> - are X Chat identity
/// key material per spec section 3.2 and land with the Chat family instead).</summary>
public sealed class UsersClient
{
    private readonly RequestExecutor _executor;

    internal UsersClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/users/{id}</c> - Get Users by ID. Requires app-only bearer, OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetUserResponse>> GetByIdAsync(GetUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<GetUserResponse>(
            HttpMethod.Get,
            $"2/users/{Uri.EscapeDataString(request.Id)}",
            FieldsQuery(request.Fields, request.Expansions, request.PostFields),
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users</c> - Get Users by IDs (up to 100 per call). Requires app-only bearer,
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetUsersListResponse>> GetByIdsAsync(GetUsersByIdsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ids.Count == 0)
        {
            throw new ArgumentException("At least one ID is required.", nameof(request));
        }

        var query = FieldsQuery(request.Fields, request.Expansions, request.PostFields);
        var withIds = new List<(string Name, string? Value)>(query.Count + 1)
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
        };
        withIds.AddRange(query);

        return _executor.SendAsync<GetUsersListResponse>(HttpMethod.Get, "2/users", withIds, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/by/username/{username}</c> - Get a User by username. Requires app-only
    /// bearer, OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetUserResponse>> GetByUsernameAsync(GetUserByUsernameRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Username);

        return _executor.SendAsync<GetUserResponse>(
            HttpMethod.Get,
            $"2/users/by/username/{Uri.EscapeDataString(request.Username)}",
            FieldsQuery(request.Fields, request.Expansions, request.PostFields),
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/by</c> - Get Users by usernames (up to 100 per call). Requires app-only
    /// bearer, OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetUsersListResponse>> GetByUsernamesAsync(GetUsersByUsernamesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Usernames.Count == 0)
        {
            throw new ArgumentException("At least one username is required.", nameof(request));
        }

        var query = FieldsQuery(request.Fields, request.Expansions, request.PostFields);
        var withUsernames = new List<(string Name, string? Value)>(query.Count + 1)
        {
            ("usernames", QueryStringBuilder.JoinCommaSeparated(request.Usernames)),
        };
        withUsernames.AddRange(query);

        return _executor.SendAsync<GetUsersListResponse>(HttpMethod.Get, "2/users/by", withUsernames, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/me</c> - Get the authenticated user. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a; app-only bearer is not accepted -
    /// there is no "me" in an app-only context.
    /// </summary>
    public Task<XResponse<GetUserResponse>> GetMeAsync(GetMyUserRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetMyUserRequest();

        return _executor.SendAsync<GetUserResponse>(
            HttpMethod.Get,
            "2/users/me",
            FieldsQuery(request.Fields, request.Expansions, request.PostFields),
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/following</c> - Follow User. Requires OAuth 2.0
    /// (<c>follows.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<FollowUserResponse>> FollowAsync(FollowUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetUserId);

        return _executor.SendAsync<FollowUserResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.SourceUserId)}/following",
            new FollowUserBody { TargetUserId = request.TargetUserId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{source_user_id}/following/{target_user_id}</c> - Unfollow User.
    /// Requires OAuth 2.0 (<c>follows.write</c> + <c>tweet.read</c> + <c>users.read</c>) or
    /// OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnfollowUserResponse>> UnfollowAsync(UnfollowUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetUserId);

        return _executor.SendAsync<UnfollowUserResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.SourceUserId)}/following/{Uri.EscapeDataString(request.TargetUserId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/muting</c> - Mute User. Requires OAuth 2.0
    /// (<c>mute.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<MuteUserResponse>> MuteAsync(MuteUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetUserId);

        return _executor.SendAsync<MuteUserResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.SourceUserId)}/muting",
            new MuteUserBody { TargetUserId = request.TargetUserId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{source_user_id}/muting/{target_user_id}</c> - Unmute User. Requires
    /// OAuth 2.0 (<c>mute.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnmuteUserResponse>> UnmuteAsync(UnmuteUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetUserId);

        return _executor.SendAsync<UnmuteUserResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.SourceUserId)}/muting/{Uri.EscapeDataString(request.TargetUserId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/likes</c> - Like Post. Requires OAuth 2.0
    /// (<c>like.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<LikePostResponse>> LikeAsync(LikePostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TweetId);

        return _executor.SendAsync<LikePostResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/likes",
            new LikePostBody { TweetId = request.TweetId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{id}/likes/{tweet_id}</c> - Unlike Post. Requires OAuth 2.0
    /// (<c>like.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnlikePostResponse>> UnlikeAsync(UnlikePostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TweetId);

        return _executor.SendAsync<UnlikePostResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/likes/{Uri.EscapeDataString(request.TweetId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/retweets</c> - Repost Post. Requires OAuth 2.0
    /// (<c>tweet.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<RepostPostResponse>> RepostAsync(RepostPostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TweetId);

        return _executor.SendAsync<RepostPostResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/retweets",
            new RepostPostBody { TweetId = request.TweetId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{id}/retweets/{source_tweet_id}</c> - Unrepost Post. Requires OAuth 2.0
    /// (<c>tweet.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnrepostPostResponse>> UnrepostAsync(UnrepostPostRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceTweetId);

        return _executor.SendAsync<UnrepostPostResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/retweets/{Uri.EscapeDataString(request.SourceTweetId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/dm/block</c> - blocks a user from sending the authenticated user
    /// Direct Messages. Requires OAuth 2.0 (<c>dm.write</c> + <c>tweet.read</c> + <c>users.read</c>)
    /// or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<BlockUserDmsResponse>> BlockDmsAsync(BlockUserDmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetUserId);

        return _executor.SendAsync<BlockUserDmsResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.TargetUserId)}/dm/block",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/dm/unblock</c> - reverses <see cref="BlockDmsAsync"/>. Requires
    /// OAuth 2.0 (<c>dm.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnblockUserDmsResponse>> UnblockDmsAsync(UnblockUserDmsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TargetUserId);

        return _executor.SendAsync<UnblockUserDmsResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.TargetUserId)}/dm/unblock",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/followed_lists</c> - Follow List. Requires OAuth 2.0
    /// (<c>list.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<FollowListResponse>> FollowListAsync(FollowListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return _executor.SendAsync<FollowListResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/followed_lists",
            new FollowListBody { ListId = request.ListId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{id}/followed_lists/{list_id}</c> - Unfollow a List. Requires OAuth 2.0
    /// (<c>list.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnfollowListResponse>> UnfollowListAsync(UnfollowListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return _executor.SendAsync<UnfollowListResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/followed_lists/{Uri.EscapeDataString(request.ListId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/pinned_lists</c> - Pin List. Requires OAuth 2.0
    /// (<c>list.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<PinListResponse>> PinListAsync(PinListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return _executor.SendAsync<PinListResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/pinned_lists",
            new PinListBody { ListId = request.ListId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{id}/pinned_lists/{list_id}</c> - Unpin a List. Requires OAuth 2.0
    /// (<c>list.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnpinListResponse>> UnpinListAsync(UnpinListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return _executor.SendAsync<UnpinListResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/pinned_lists/{Uri.EscapeDataString(request.ListId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/bookmarks</c> (single-Post form) - Create Bookmark. Requires OAuth
    /// 2.0 (<c>bookmark.write</c> + <c>tweet.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<CreateBookmarkResponse>> CreateBookmarkAsync(CreateBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TweetId);

        return _executor.SendAsync<CreateBookmarkResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/bookmarks",
            new CreateBookmarkBody { TweetId = request.TweetId, FolderId = request.FolderId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/bookmarks</c> (batch form, up to 25 Post IDs) - Create Bookmarks.
    /// Requires OAuth 2.0 (<c>bookmark.write</c> + <c>tweet.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<CreateBookmarksResponse>> CreateBookmarksAsync(CreateBookmarksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        if (request.TweetIds.Count == 0)
        {
            throw new ArgumentException("At least one tweet ID is required.", nameof(request));
        }

        return _executor.SendAsync<CreateBookmarksResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/bookmarks",
            new CreateBookmarksBody { TweetIds = request.TweetIds, FolderId = request.FolderId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/users/{id}/bookmarks/{tweet_id}</c> - Delete Bookmark. Requires OAuth 2.0
    /// (<c>bookmark.write</c> + <c>tweet.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<DeleteBookmarkResponse>> DeleteBookmarkAsync(DeleteBookmarkRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TweetId);

        return _executor.SendAsync<DeleteBookmarkResponse>(
            HttpMethod.Delete,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/bookmarks/{Uri.EscapeDataString(request.TweetId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/users/{id}/bookmarks/folders</c> - Create Bookmark Folder. Requires OAuth 2.0
    /// (<c>bookmark.write</c> + <c>users.read</c>). Returns HTTP 201.
    /// </summary>
    public Task<XResponse<CreateBookmarkFolderResponse>> CreateBookmarkFolderAsync(CreateBookmarkFolderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        return _executor.SendAsync<CreateBookmarkFolderResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/bookmarks/folders",
            new CreateBookmarkFolderBody { Name = request.Name },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/followers</c> - one page. Requires app-only bearer, OAuth
    /// 2.0 (<c>follows.read</c> + <c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<GetUsersPageResponse>> GetFollowersPageAsync(GetUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchUsersPageAsync(FollowersPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/followers</c> - lazy page-by-page traversal (spec section 14).</summary>
    public IAsyncEnumerable<XResponse<GetUsersPageResponse>> GetFollowersPagesAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsersPages(FollowersPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/followers</c> - lazy item traversal (spec section 14).</summary>
    public IAsyncEnumerable<User> GetFollowersAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsers(FollowersPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/following</c> - one page. Requires OAuth 2.0
    /// (<c>follows.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a; app-only
    /// bearer is also accepted per the registry.</summary>
    public Task<XResponse<GetUsersPageResponse>> GetFollowingPageAsync(GetUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchUsersPageAsync(FollowingPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/following</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetUsersPageResponse>> GetFollowingPagesAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsersPages(FollowingPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/following</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetFollowingAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsers(FollowingPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/blocking</c> - one page. Requires OAuth 2.0
    /// (<c>block.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetUsersPageResponse>> GetBlockingPageAsync(GetUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchUsersPageAsync(BlockingPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/blocking</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetUsersPageResponse>> GetBlockingPagesAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsersPages(BlockingPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/blocking</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetBlockingAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsers(BlockingPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/muting</c> - one page. Requires OAuth 2.0
    /// (<c>mute.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetUsersPageResponse>> GetMutingPageAsync(GetUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchUsersPageAsync(MutingPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/muting</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetUsersPageResponse>> GetMutingPagesAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsersPages(MutingPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/muting</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetMutingAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsers(MutingPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/affiliates</c> - one page. Requires app-only bearer or
    /// OAuth 2.0 (<c>users.read</c> + <c>tweet.read</c>).</summary>
    public Task<XResponse<GetUsersPageResponse>> GetAffiliatesPageAsync(GetUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchUsersPageAsync(AffiliatesPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/affiliates</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetUsersPageResponse>> GetAffiliatesPagesAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsersPages(AffiliatesPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/affiliates</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetAffiliatesAsync(GetUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateUsers(AffiliatesPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/search</c> - one page. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetUsersPageResponse>> SearchPageAsync(SearchUsersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return FetchSearchUsersPageAsync(request, request.NextToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/search</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetUsersPageResponse>> SearchPagesAsync(SearchUsersRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return XPaginator.EnumeratePagesAsync<GetUsersPageResponse>(
            (token, ct) => FetchSearchUsersPageAsync(request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/users/search</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> SearchAsync(SearchUsersRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return XPaginator.EnumerateItemsAsync<GetUsersPageResponse, User>(
            (token, ct) => FetchSearchUsersPageAsync(request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    private static string FollowersPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/followers";

    private static string FollowingPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/following";

    private static string BlockingPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/blocking";

    private static string MutingPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/muting";

    private static string AffiliatesPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/affiliates";

    private IAsyncEnumerable<XResponse<GetUsersPageResponse>> EnumerateUsersPages(string path, GetUsersPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<GetUsersPageResponse>(
            (token, ct) => FetchUsersPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<User> EnumerateUsers(string path, GetUsersPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<GetUsersPageResponse, User>(
            (token, ct) => FetchUsersPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<GetUsersPageResponse>> FetchUsersPageAsync(string path, GetUsersPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
        };
        query.AddRange(FieldsQuery(request.Fields, request.Expansions, request.PostFields));

        return _executor.SendAsync<GetUsersPageResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    private Task<XResponse<GetUsersPageResponse>> FetchSearchUsersPageAsync(SearchUsersRequest request, string? nextToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("query", request.Query),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("next_token", nextToken),
        };
        query.AddRange(FieldsQuery(request.Fields, request.Expansions, request.PostFields));

        return _executor.SendAsync<GetUsersPageResponse>(HttpMethod.Get, "2/users/search", query, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/tweets</c> - Get Users Posts. Requires app-only bearer,
    /// OAuth 2.0 (<c>users.read</c> + <c>tweet.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<GetPostsPageResponse>> GetPostsPageAsync(GetPostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchPostsPageAsync(PostsPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/tweets</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostsPageResponse>> GetPostsPagesAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePostsPages(PostsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/tweets</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetPostsAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePosts(PostsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/mentions</c> - one page. Requires app-only bearer, OAuth
    /// 2.0 (<c>users.read</c> + <c>tweet.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<GetPostsPageResponse>> GetMentionsPageAsync(GetPostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchPostsPageAsync(MentionsPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/mentions</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostsPageResponse>> GetMentionsPagesAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePostsPages(MentionsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/mentions</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetMentionsAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePosts(MentionsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/timelines/reverse_chronological</c> - one page. Requires
    /// OAuth 2.0 (<c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetPostsPageResponse>> GetTimelinePageAsync(GetPostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchPostsPageAsync(TimelinePath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/timelines/reverse_chronological</c> - lazy page-by-page
    /// traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostsPageResponse>> GetTimelinePagesAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePostsPages(TimelinePath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/timelines/reverse_chronological</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetTimelineAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePosts(TimelinePath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/liked_tweets</c> - one page. Requires OAuth 2.0
    /// (<c>like.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetPostsPageResponse>> GetLikedPostsPageAsync(GetPostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchPostsPageAsync(LikedPostsPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/liked_tweets</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostsPageResponse>> GetLikedPostsPagesAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePostsPages(LikedPostsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/liked_tweets</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetLikedPostsAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePosts(LikedPostsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/bookmarks</c> - one page. Requires OAuth 2.0
    /// (<c>bookmark.read</c> + <c>tweet.read</c> + <c>users.read</c>).</summary>
    public Task<XResponse<GetPostsPageResponse>> GetBookmarksPageAsync(GetPostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchPostsPageAsync(BookmarksPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/bookmarks</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostsPageResponse>> GetBookmarksPagesAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePostsPages(BookmarksPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/bookmarks</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetBookmarksAsync(GetPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumeratePosts(BookmarksPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/reposts_of_me</c> - one page, for the authenticated user.
    /// Requires OAuth 2.0 (<c>timeline.read</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetPostsPageResponse>> GetRepostsOfMePageAsync(GetRepostsOfMeRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetRepostsOfMeRequest();

        return FetchRepostsOfMePageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/reposts_of_me</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetPostsPageResponse>> GetRepostsOfMePagesAsync(GetRepostsOfMeRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetRepostsOfMeRequest();

        return XPaginator.EnumeratePagesAsync<GetPostsPageResponse>(
            (token, ct) => FetchRepostsOfMePageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/users/reposts_of_me</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetRepostsOfMeAsync(GetRepostsOfMeRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetRepostsOfMeRequest();

        return XPaginator.EnumerateItemsAsync<GetPostsPageResponse, Post>(
            (token, ct) => FetchRepostsOfMePageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/followed_lists</c> - one page. Requires app-only bearer,
    /// OAuth 2.0 (<c>list.read</c> + <c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<GetListsPageResponse>> GetFollowedListsPageAsync(GetListsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchListsPageAsync(FollowedListsPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/followed_lists</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetListsPageResponse>> GetFollowedListsPagesAsync(GetListsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateListsPages(FollowedListsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/followed_lists</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<XList> GetFollowedListsAsync(GetListsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateLists(FollowedListsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/list_memberships</c> - one page. Requires app-only bearer,
    /// OAuth 2.0 (<c>list.read</c> + <c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<GetListsPageResponse>> GetListMembershipsPageAsync(GetListsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchListsPageAsync(ListMembershipsPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/list_memberships</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetListsPageResponse>> GetListMembershipsPagesAsync(GetListsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateListsPages(ListMembershipsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/list_memberships</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<XList> GetListMembershipsAsync(GetListsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateLists(ListMembershipsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/owned_lists</c> - one page. Requires app-only bearer,
    /// OAuth 2.0 (<c>list.read</c> + <c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<GetListsPageResponse>> GetOwnedListsPageAsync(GetListsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return FetchListsPageAsync(OwnedListsPath(request.UserId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/owned_lists</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetListsPageResponse>> GetOwnedListsPagesAsync(GetListsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateListsPages(OwnedListsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/owned_lists</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<XList> GetOwnedListsAsync(GetListsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return EnumerateLists(OwnedListsPath(request.UserId), request, options, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/{id}/pinned_lists</c> - Get Users Pinned Lists. Not paginated - see
    /// <see cref="GetPinnedListsRequest"/>. Requires OAuth 2.0
    /// (<c>list.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetListsPageResponse>> GetPinnedListsAsync(GetPinnedListsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        var query = new List<(string Name, string? Value)>();
        query.AddRange(ListFieldsQuery(request.Fields, request.Expansions, request.UserFields));

        return _executor.SendAsync<GetListsPageResponse>(
            HttpMethod.Get,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/pinned_lists",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/{id}/bookmarks/folders</c> - Get Users Bookmark Folders. Not paginated -
    /// see <see cref="GetBookmarkFoldersRequest"/>. Requires OAuth 2.0
    /// (<c>bookmark.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<GetBookmarkFoldersResponse>> GetBookmarkFoldersAsync(GetBookmarkFoldersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", request.PaginationToken),
        };

        return _executor.SendAsync<GetBookmarkFoldersResponse>(
            HttpMethod.Get,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/bookmarks/folders",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/{id}/bookmarks/folders/{folder_id}</c> - Get Users Bookmarks by Folder ID.
    /// Not paginated - see <see cref="GetBookmarksByFolderRequest"/>. Requires OAuth 2.0
    /// (<c>bookmark.read</c> + <c>users.read</c> + <c>tweet.read</c>).
    /// </summary>
    public Task<XResponse<GetBookmarksByFolderResponse>> GetBookmarksByFolderAsync(GetBookmarksByFolderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FolderId);

        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", request.PaginationToken),
        };

        return _executor.SendAsync<GetBookmarksByFolderResponse>(
            HttpMethod.Get,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/bookmarks/folders/{Uri.EscapeDataString(request.FolderId)}",
            query,
            cancellationToken);
    }

    private static string PostsPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/tweets";

    private static string MentionsPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/mentions";

    private static string TimelinePath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/timelines/reverse_chronological";

    private static string LikedPostsPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/liked_tweets";

    private static string BookmarksPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/bookmarks";

    private static string FollowedListsPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/followed_lists";

    private static string ListMembershipsPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/list_memberships";

    private static string OwnedListsPath(string userId) => $"2/users/{Uri.EscapeDataString(userId)}/owned_lists";

    private IAsyncEnumerable<XResponse<GetPostsPageResponse>> EnumeratePostsPages(string path, GetPostsPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<GetPostsPageResponse>(
            (token, ct) => FetchPostsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<Post> EnumeratePosts(string path, GetPostsPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<GetPostsPageResponse, Post>(
            (token, ct) => FetchPostsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<GetPostsPageResponse>> FetchPostsPageAsync(string path, GetPostsPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("since_id", request.SinceId),
            ("until_id", request.UntilId),
            ("exclude", QueryStringBuilder.JoinCommaSeparated(request.Exclude, e => e.ToApiValue())),
        };
        query.AddRange(PostFieldsQuery(request.Fields, request.Expansions));

        return _executor.SendAsync<GetPostsPageResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    private Task<XResponse<GetPostsPageResponse>> FetchRepostsOfMePageAsync(GetRepostsOfMeRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
        };
        query.AddRange(PostFieldsQuery(request.Fields, request.Expansions));

        return _executor.SendAsync<GetPostsPageResponse>(HttpMethod.Get, "2/users/reposts_of_me", query, cancellationToken);
    }

    private IAsyncEnumerable<XResponse<GetListsPageResponse>> EnumerateListsPages(string path, GetListsPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<GetListsPageResponse>(
            (token, ct) => FetchListsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<XList> EnumerateLists(string path, GetListsPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<GetListsPageResponse, XList>(
            (token, ct) => FetchListsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<GetListsPageResponse>> FetchListsPageAsync(string path, GetListsPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
        };
        query.AddRange(ListFieldsQuery(request.Fields, request.Expansions, request.UserFields));

        return _executor.SendAsync<GetListsPageResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    /// <summary>Builds the <c>post.fields</c>/<c>expansions</c> query parameters shared by the
    /// Posts-list operations - these don't expose <c>user.fields</c> in the registry.</summary>
    private static IReadOnlyList<(string Name, string? Value)> PostFieldsQuery(
        IReadOnlyCollection<XPostField>? fields,
        IReadOnlyCollection<XExpansion>? expansions) =>
    [
        ("post.fields", QueryStringBuilder.JoinCommaSeparated(fields, f => f.ToApiValue())),
        ("expansions", QueryStringBuilder.JoinCommaSeparated(expansions, e => e.ToApiValue())),
    ];

    /// <summary>Builds the <c>list.fields</c>/<c>expansions</c>/<c>user.fields</c> query
    /// parameters shared by the Lists-list operations.</summary>
    private static IReadOnlyList<(string Name, string? Value)> ListFieldsQuery(
        IReadOnlyCollection<XListField>? fields,
        IReadOnlyCollection<XExpansion>? expansions,
        IReadOnlyCollection<XUserField>? userFields) =>
    [
        ("list.fields", QueryStringBuilder.JoinCommaSeparated(fields, f => f.ToApiValue())),
        ("expansions", QueryStringBuilder.JoinCommaSeparated(expansions, e => e.ToApiValue())),
        ("user.fields", QueryStringBuilder.JoinCommaSeparated(userFields, f => f.ToApiValue())),
    ];

    /// <summary>Builds the <c>user.fields</c>/<c>expansions</c>/<c>post.fields</c> query
    /// parameters shared by every plain-lookup Users operation (SER-05).</summary>
    private static IReadOnlyList<(string Name, string? Value)> FieldsQuery(
        IReadOnlyCollection<XUserField>? fields,
        IReadOnlyCollection<XExpansion>? expansions,
        IReadOnlyCollection<XPostField>? postFields) =>
    [
        ("user.fields", QueryStringBuilder.JoinCommaSeparated(fields, f => f.ToApiValue())),
        ("expansions", QueryStringBuilder.JoinCommaSeparated(expansions, e => e.ToApiValue())),
        ("post.fields", QueryStringBuilder.JoinCommaSeparated(postFields, f => f.ToApiValue())),
    ];
}

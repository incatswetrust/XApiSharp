using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.Lists;

/// <summary>Typed methods for the Lists family (9 operations per the registry).</summary>
public sealed class ListsClient
{
    private readonly RequestExecutor _executor;

    internal ListsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>POST /2/lists</c> - Create Lists. Requires OAuth 2.0
    /// (<c>list.write</c> + <c>tweet.read</c> + <c>list.read</c> + <c>users.read</c>) or OAuth
    /// 1.0a. Returns HTTP 201.
    /// </summary>
    public Task<XResponse<CreateListResponse>> CreateAsync(CreateListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);

        return _executor.SendAsync<CreateListResponse>(
            HttpMethod.Post,
            "2/lists",
            new CreateListBody { Name = request.Name, Private = request.Private },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/lists/{id}</c> - Delete List. Requires OAuth 2.0
    /// (<c>list.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<DeleteListResponse>> DeleteAsync(DeleteListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<DeleteListResponse>(
            HttpMethod.Delete,
            $"2/lists/{Uri.EscapeDataString(request.Id)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/lists/{id}</c> - Get Lists by ID. Requires app-only bearer, OAuth 2.0
    /// (<c>list.read</c> + <c>users.read</c> + <c>tweet.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetListResponse>> GetByIdAsync(GetListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>();
        query.AddRange(ListFieldsQuery(request.Fields, request.Expansions, request.UserFields));

        return _executor.SendAsync<GetListResponse>(
            HttpMethod.Get,
            $"2/lists/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>PUT /2/lists/{id}</c> - Update List. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c> + <c>list.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UpdateListResponse>> UpdateAsync(UpdateListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<UpdateListResponse>(
            HttpMethod.Put,
            $"2/lists/{Uri.EscapeDataString(request.Id)}",
            new UpdateListBody { Name = request.Name, Private = request.Private },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/followers</c> - one page. Requires app-only bearer, OAuth
    /// 2.0 (<c>tweet.read</c> + <c>users.read</c> + <c>list.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<ListUsersPageResponse>> GetFollowersPageAsync(ListUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return FetchListUsersPageAsync(FollowersPath(request.ListId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/followers</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<ListUsersPageResponse>> GetFollowersPagesAsync(ListUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return EnumerateListUsersPages(FollowersPath(request.ListId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/followers</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetFollowersAsync(ListUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return EnumerateListUsers(FollowersPath(request.ListId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/members</c> - one page. Requires app-only bearer, OAuth
    /// 2.0 (<c>tweet.read</c> + <c>users.read</c> + <c>list.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<ListUsersPageResponse>> GetMembersPageAsync(ListUsersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return FetchListUsersPageAsync(MembersPath(request.ListId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/members</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<ListUsersPageResponse>> GetMembersPagesAsync(ListUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return EnumerateListUsersPages(MembersPath(request.ListId), request, options, cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/members</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetMembersAsync(ListUsersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return EnumerateListUsers(MembersPath(request.ListId), request, options, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/lists/{id}/members</c> - Add List Member. Requires OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c> + <c>list.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<AddListMemberResponse>> AddMemberAsync(AddListMemberRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return _executor.SendAsync<AddListMemberResponse>(
            HttpMethod.Post,
            $"2/lists/{Uri.EscapeDataString(request.ListId)}/members",
            new AddListMemberBody { UserId = request.UserId },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/lists/{id}/members/{user_id}</c> - Remove a List member. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c> + <c>list.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<RemoveListMemberResponse>> RemoveMemberAsync(RemoveListMemberRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return _executor.SendAsync<RemoveListMemberResponse>(
            HttpMethod.Delete,
            $"2/lists/{Uri.EscapeDataString(request.ListId)}/members/{Uri.EscapeDataString(request.UserId)}",
            cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/tweets</c> - one page. Requires app-only bearer, OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c> + <c>list.read</c>), or OAuth 1.0a.</summary>
    public Task<XResponse<ListPostsPageResponse>> GetPostsPageAsync(ListPostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return FetchListPostsPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/tweets</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<ListPostsPageResponse>> GetPostsPagesAsync(ListPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return XPaginator.EnumeratePagesAsync<ListPostsPageResponse>(
            (token, ct) => FetchListPostsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/lists/{id}/tweets</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetPostsAsync(ListPostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ListId);

        return XPaginator.EnumerateItemsAsync<ListPostsPageResponse, Post>(
            (token, ct) => FetchListPostsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    private static string FollowersPath(string listId) => $"2/lists/{Uri.EscapeDataString(listId)}/followers";

    private static string MembersPath(string listId) => $"2/lists/{Uri.EscapeDataString(listId)}/members";

    private IAsyncEnumerable<XResponse<ListUsersPageResponse>> EnumerateListUsersPages(string path, ListUsersPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<ListUsersPageResponse>(
            (token, ct) => FetchListUsersPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<User> EnumerateListUsers(string path, ListUsersPageRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<ListUsersPageResponse, User>(
            (token, ct) => FetchListUsersPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<ListUsersPageResponse>> FetchListUsersPageAsync(string path, ListUsersPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(request.PostFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<ListUsersPageResponse>(HttpMethod.Get, path, query, cancellationToken);
    }

    private Task<XResponse<ListPostsPageResponse>> FetchListPostsPageAsync(ListPostsPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var fields = request.Fields;
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PostFields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(fields?.Expansions, e => e.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(fields?.UserFields, f => f.ToApiValue())),
            ("media.fields", QueryStringBuilder.JoinCommaSeparated(fields?.MediaFields, f => f.ToApiValue())),
            ("poll.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PollFields, f => f.ToApiValue())),
            ("place.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PlaceFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<ListPostsPageResponse>(
            HttpMethod.Get,
            $"2/lists/{Uri.EscapeDataString(request.ListId)}/tweets",
            query,
            cancellationToken);
    }

    private static IReadOnlyList<(string Name, string? Value)> ListFieldsQuery(
        IReadOnlyCollection<XListField>? fields,
        IReadOnlyCollection<XExpansion>? expansions,
        IReadOnlyCollection<XUserField>? userFields) =>
    [
        ("list.fields", QueryStringBuilder.JoinCommaSeparated(fields, f => f.ToApiValue())),
        ("expansions", QueryStringBuilder.JoinCommaSeparated(expansions, e => e.ToApiValue())),
        ("user.fields", QueryStringBuilder.JoinCommaSeparated(userFields, f => f.ToApiValue())),
    ];
}

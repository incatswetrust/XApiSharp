using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.Spaces;

/// <summary>Typed methods for the Spaces family (6 operations per the registry).</summary>
public sealed class SpacesClient
{
    private readonly RequestExecutor _executor;

    internal SpacesClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/spaces/{id}</c> - Get Spaces by ID. Requires app-only bearer or OAuth 2.0
    /// (<c>space.read</c> + <c>tweet.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<GetSpaceResponse>> GetByIdAsync(GetSpaceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<GetSpaceResponse>(
            HttpMethod.Get,
            $"2/spaces/{Uri.EscapeDataString(request.Id)}",
            SpaceFieldsQuery(request.Fields),
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/spaces</c> - Get Spaces by IDs (up to 100 per call). Requires app-only bearer or
    /// OAuth 2.0 (<c>space.read</c> + <c>tweet.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<GetSpacesListResponse>> GetByIdsAsync(GetSpacesByIdsRequest request, CancellationToken cancellationToken = default)
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
        query.AddRange(SpaceFieldsQuery(request.Fields));

        return _executor.SendAsync<GetSpacesListResponse>(HttpMethod.Get, "2/spaces", query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/spaces/by/creator_ids</c> - Get Spaces by Creator IDs (up to 100 per call).
    /// Requires app-only bearer or OAuth 2.0 (<c>space.read</c> + <c>users.read</c> + <c>tweet.read</c>).
    /// </summary>
    public Task<XResponse<GetSpacesListResponse>> GetByCreatorIdsAsync(GetSpacesByCreatorIdsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.UserIds.Count == 0)
        {
            throw new ArgumentException("At least one user ID is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("user_ids", QueryStringBuilder.JoinCommaSeparated(request.UserIds)),
        };
        query.AddRange(SpaceFieldsQuery(request.Fields));

        return _executor.SendAsync<GetSpacesListResponse>(HttpMethod.Get, "2/spaces/by/creator_ids", query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/spaces/search</c> - Search Spaces. Requires app-only bearer or OAuth 2.0
    /// (<c>space.read</c> + <c>tweet.read</c> + <c>users.read</c>).
    /// </summary>
    public Task<XResponse<GetSpacesListResponse>> SearchAsync(SearchSpacesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var query = new List<(string Name, string? Value)>
        {
            ("query", request.Query),
            ("state", request.State?.ToApiValue()),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
        };
        query.AddRange(SpaceFieldsQuery(request.Fields));

        return _executor.SendAsync<GetSpacesListResponse>(HttpMethod.Get, "2/spaces/search", query, cancellationToken);
    }

    /// <summary><c>GET /2/spaces/{id}/buyers</c> - one page. Requires OAuth 2.0
    /// (<c>space.read</c> + <c>users.read</c> + <c>tweet.read</c>).</summary>
    public Task<XResponse<SpaceBuyersPageResponse>> GetBuyersPageAsync(SpaceBuyersPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpaceId);

        return FetchBuyersPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/spaces/{id}/buyers</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SpaceBuyersPageResponse>> GetBuyersPagesAsync(SpaceBuyersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpaceId);

        return XPaginator.EnumeratePagesAsync<SpaceBuyersPageResponse>(
            (token, ct) => FetchBuyersPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/spaces/{id}/buyers</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<User> GetBuyersAsync(SpaceBuyersPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpaceId);

        return XPaginator.EnumerateItemsAsync<SpaceBuyersPageResponse, User>(
            (token, ct) => FetchBuyersPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/spaces/{id}/tweets</c> - one page. Requires app-only bearer or OAuth
    /// 2.0 (<c>tweet.read</c> + <c>space.read</c> + <c>users.read</c>).</summary>
    public Task<XResponse<SpacePostsPageResponse>> GetPostsPageAsync(SpacePostsPageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpaceId);

        return FetchPostsPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/spaces/{id}/tweets</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SpacePostsPageResponse>> GetPostsPagesAsync(SpacePostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpaceId);

        return XPaginator.EnumeratePagesAsync<SpacePostsPageResponse>(
            (token, ct) => FetchPostsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/spaces/{id}/tweets</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> GetPostsAsync(SpacePostsPageRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SpaceId);

        return XPaginator.EnumerateItemsAsync<SpacePostsPageResponse, Post>(
            (token, ct) => FetchPostsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    private Task<XResponse<SpaceBuyersPageResponse>> FetchBuyersPageAsync(SpaceBuyersPageRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(request.PostFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<SpaceBuyersPageResponse>(
            HttpMethod.Get,
            $"2/spaces/{Uri.EscapeDataString(request.SpaceId)}/buyers",
            query,
            cancellationToken);
    }

    private Task<XResponse<SpacePostsPageResponse>> FetchPostsPageAsync(SpacePostsPageRequest request, string? paginationToken, CancellationToken cancellationToken)
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

        return _executor.SendAsync<SpacePostsPageResponse>(
            HttpMethod.Get,
            $"2/spaces/{Uri.EscapeDataString(request.SpaceId)}/tweets",
            query,
            cancellationToken);
    }

    private static IReadOnlyList<(string Name, string? Value)> SpaceFieldsQuery(SpaceFieldSelection? fields) =>
    [
        ("space.fields", QueryStringBuilder.JoinCommaSeparated(fields?.SpaceFields, f => f.ToApiValue())),
        ("expansions", QueryStringBuilder.JoinCommaSeparated(fields?.Expansions, e => e.ToApiValue())),
        ("user.fields", QueryStringBuilder.JoinCommaSeparated(fields?.UserFields, f => f.ToApiValue())),
        ("topic.fields", QueryStringBuilder.JoinCommaSeparated(fields?.TopicFields, f => f.ToApiValue())),
    ];
}

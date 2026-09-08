using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.Communities;

/// <summary>Typed methods for the Communities family (2 operations per the registry).</summary>
public sealed class CommunitiesClient
{
    private readonly RequestExecutor _executor;

    internal CommunitiesClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/communities/{id}</c> - Get Communities by ID. Requires app-only bearer or OAuth
    /// 2.0 (<c>list.read</c> + <c>users.read</c> + <c>tweet.read</c>).
    /// </summary>
    public Task<XResponse<GetCommunityResponse>> GetByIdAsync(GetCommunityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("community.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetCommunityResponse>(
            HttpMethod.Get,
            $"2/communities/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary><c>GET /2/communities/search</c> - one page. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<SearchCommunitiesResponse>> SearchPageAsync(SearchCommunitiesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return FetchSearchPageAsync(request, request.NextToken, cancellationToken);
    }

    /// <summary><c>GET /2/communities/search</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SearchCommunitiesResponse>> SearchPagesAsync(SearchCommunitiesRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return XPaginator.EnumeratePagesAsync<SearchCommunitiesResponse>(
            (token, ct) => FetchSearchPageAsync(request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/communities/search</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Community> SearchAsync(SearchCommunitiesRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        return XPaginator.EnumerateItemsAsync<SearchCommunitiesResponse, Community>(
            (token, ct) => FetchSearchPageAsync(request, token ?? request.NextToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    private Task<XResponse<SearchCommunitiesResponse>> FetchSearchPageAsync(SearchCommunitiesRequest request, string? nextToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("query", request.Query),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("next_token", nextToken),
            ("community.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<SearchCommunitiesResponse>(HttpMethod.Get, "2/communities/search", query, cancellationToken);
    }
}

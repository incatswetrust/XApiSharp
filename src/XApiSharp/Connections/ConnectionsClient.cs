using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.Connections;

/// <summary>
/// Typed methods for the Connections family (4 operations per the registry) - rule/config
/// management for the underlying stream TCP connections (by uuid/endpoint), not the stream
/// connection itself (Stream family, E5).
/// </summary>
public sealed class ConnectionsClient
{
    private readonly RequestExecutor _executor;

    internal ConnectionsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>DELETE /2/connections</c> - Terminate multiple connections (up to 100 UUIDs per call).
    /// Requires app-only bearer.
    /// </summary>
    public Task<XResponse<TerminateConnectionsResponse>> DeleteByUuidsAsync(DeleteConnectionsByUuidsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Uuids.Count == 0)
        {
            throw new ArgumentException("At least one UUID is required.", nameof(request));
        }

        return _executor.SendAsync<TerminateConnectionsResponse>(
            HttpMethod.Delete,
            "2/connections",
            new DeleteConnectionsByUuidsBody { Uuids = request.Uuids },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/connections</c> - one page. Requires app-only bearer.</summary>
    public Task<XResponse<GetConnectionHistoryResponse>> GetHistoryPageAsync(GetConnectionHistoryRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetConnectionHistoryRequest();

        return FetchHistoryPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/connections</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetConnectionHistoryResponse>> GetHistoryPagesAsync(GetConnectionHistoryRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetConnectionHistoryRequest();

        return XPaginator.EnumeratePagesAsync<GetConnectionHistoryResponse>(
            (token, ct) => FetchHistoryPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/connections</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Connection> GetHistoryAsync(GetConnectionHistoryRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetConnectionHistoryRequest();

        return XPaginator.EnumerateItemsAsync<GetConnectionHistoryResponse, Connection>(
            (token, ct) => FetchHistoryPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/connections/all</c> - Terminate all connections. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<TerminateConnectionsResponse>> DeleteAllAsync(CancellationToken cancellationToken = default) =>
        _executor.SendAsync<TerminateConnectionsResponse>(HttpMethod.Delete, "2/connections/all", cancellationToken);

    /// <summary>
    /// <c>DELETE /2/connections/{endpoint_id}</c> - Terminate connections by endpoint. Requires
    /// app-only bearer.
    /// </summary>
    public Task<XResponse<TerminateConnectionsResponse>> DeleteByEndpointAsync(DeleteConnectionsByEndpointRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _executor.SendAsync<TerminateConnectionsResponse>(
            HttpMethod.Delete,
            $"2/connections/{request.EndpointId.ToApiValue()}",
            cancellationToken);
    }

    private Task<XResponse<GetConnectionHistoryResponse>> FetchHistoryPageAsync(GetConnectionHistoryRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("status", request.Status?.ToApiValue()),
            ("endpoints", QueryStringBuilder.JoinCommaSeparated(request.Endpoints, e => e.ToApiValue())),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("connection.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetConnectionHistoryResponse>(HttpMethod.Get, "2/connections", query, cancellationToken);
    }
}

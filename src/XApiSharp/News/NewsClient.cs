using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.News;

/// <summary>Typed methods for the News family (2 operations per the registry).</summary>
public sealed class NewsClient
{
    private readonly RequestExecutor _executor;

    internal NewsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/news/{id}</c> - Get news stories by ID. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetNewsResponse>> GetByIdAsync(GetNewsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("news.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetNewsResponse>(
            HttpMethod.Get,
            $"2/news/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/news/search</c> - Search News. Requires app-only bearer or OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c>).
    /// </summary>
    public Task<XResponse<SearchNewsResponse>> SearchAsync(SearchNewsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var query = new List<(string Name, string? Value)>
        {
            ("query", request.Query),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("max_age_hours", request.MaxAgeHours?.ToString(CultureInfo.InvariantCulture)),
            ("news.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<SearchNewsResponse>(HttpMethod.Get, "2/news/search", query, cancellationToken);
    }
}

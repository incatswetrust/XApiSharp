using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Trends;

/// <summary>Typed methods for the Trends family (2 operations per the registry).</summary>
public sealed class TrendsClient
{
    private readonly RequestExecutor _executor;

    internal TrendsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/trends/by/woeid/{woeid}</c> - Get Trends by Woeid. Requires app-only bearer or
    /// OAuth 2.0 (no documented scopes beyond authentication).
    /// </summary>
    public Task<XResponse<GetTrendsByWoeidResponse>> GetByWoeidAsync(GetTrendsByWoeidRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("max_trends", request.MaxTrends?.ToString(CultureInfo.InvariantCulture)),
            ("trend.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetTrendsByWoeidResponse>(
            HttpMethod.Get,
            $"2/trends/by/woeid/{request.Woeid.ToString(CultureInfo.InvariantCulture)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/users/personalized_trends</c> - Get personalized trends for the authenticated
    /// user. Requires OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetPersonalizedTrendsResponse>> GetPersonalizedAsync(GetPersonalizedTrendsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetPersonalizedTrendsRequest();

        var query = new List<(string Name, string? Value)>
        {
            ("personalized_trend.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetPersonalizedTrendsResponse>(HttpMethod.Get, "2/users/personalized_trends", query, cancellationToken);
    }
}

using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Usage;

/// <summary>Typed methods for the Usage family (2 operations per the registry).</summary>
public sealed class UsageClient
{
    private readonly RequestExecutor _executor;

    internal UsageClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/usage/credits</c> - Get usage credits. Requires app-only bearer or OAuth 2.0
    /// (no documented scopes beyond authentication).
    /// </summary>
    public Task<XResponse<GetUsageCreditsResponse>> GetCreditsAsync(CancellationToken cancellationToken = default) =>
        _executor.SendAsync<GetUsageCreditsResponse>(HttpMethod.Get, "2/usage/credits", cancellationToken);

    /// <summary>
    /// <c>GET /2/usage/tweets</c> - Get Usage. Requires app-only bearer only, per the registry -
    /// no OAuth 2.0 user-token variant is declared.
    /// </summary>
    public Task<XResponse<GetUsageResponse>> GetUsageAsync(GetUsageRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetUsageRequest();

        var query = new List<(string Name, string? Value)>
        {
            ("days", request.Days?.ToString(CultureInfo.InvariantCulture)),
            ("usage.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetUsageResponse>(HttpMethod.Get, "2/usage/tweets", query, cancellationToken);
    }
}

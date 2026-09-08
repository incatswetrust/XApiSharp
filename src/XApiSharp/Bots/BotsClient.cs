using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Bots;

/// <summary>
/// Typed methods for the Bots family (6 operations per the registry) - bot account create/
/// update/delete/token management. Flagged tentative at E0 inventory time (undocumented on
/// docs.x.com, present in the OpenAPI snapshot only) and confirmed in scope for E4 per project
/// owner decision.
/// </summary>
public sealed class BotsClient
{
    private readonly RequestExecutor _executor;

    internal BotsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/bots</c> - Get Bots. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<GetBotsResponse>> GetAllAsync(GetBotsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetBotsRequest();

        var query = new List<(string Name, string? Value)>
        {
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(request.PostFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetBotsResponse>(HttpMethod.Get, "2/bots", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/bots</c> - Create a bot. Requires app-only bearer. Returns HTTP 201.
    /// </summary>
    public Task<XResponse<CreateBotResponse>> CreateAsync(CreateBotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Handle);

        var body = new CreateBotBody { Handle = request.Handle, DisplayName = request.DisplayName, Scopes = request.Scopes };

        return _executor.SendAsync<CreateBotResponse>(HttpMethod.Post, "2/bots", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/bots/{id}</c> - Delete Bot. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<DeleteBotResponse>> DeleteAsync(DeleteBotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<DeleteBotResponse>(
            HttpMethod.Delete,
            $"2/bots/{Uri.EscapeDataString(request.Id)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>PUT /2/bots/{id}</c> - Update Bot. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<UpdateBotResponse>> UpdateAsync(UpdateBotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var body = new UpdateBotBody
        {
            Handle = request.Handle,
            DisplayName = request.DisplayName,
            DmPermission = request.DmPermission?.ToApiValue(),
        };

        return _executor.SendAsync<UpdateBotResponse>(
            HttpMethod.Put,
            $"2/bots/{Uri.EscapeDataString(request.Id)}",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/bots/{id}/token</c> - Revoke Bot Token. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<RevokeBotTokenResponse>> RevokeTokenAsync(RevokeBotTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<RevokeBotTokenResponse>(
            HttpMethod.Delete,
            $"2/bots/{Uri.EscapeDataString(request.Id)}/token",
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/bots/{id}/token</c> - Rotate Bot Token (invalidates every previously issued
    /// token for this bot). Requires app-only bearer.
    /// </summary>
    public Task<XResponse<RotateBotTokenResponse>> RotateTokenAsync(RotateBotTokenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<RotateBotTokenResponse>(
            HttpMethod.Post,
            $"2/bots/{Uri.EscapeDataString(request.Id)}/token",
            new RotateBotTokenBody { Scopes = request.Scopes },
            queryParameters: null,
            cancellationToken);
    }
}

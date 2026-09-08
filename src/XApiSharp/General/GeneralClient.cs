using System.Text.Json;
using XApiSharp.Transport;

namespace XApiSharp.General;

/// <summary>Typed methods for the General family (1 operation per the registry) - operations that
/// don't belong to any user-facing entity.</summary>
public sealed class GeneralClient
{
    private readonly RequestExecutor _executor;

    internal GeneralClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/openapi.json</c> - Get OpenAPI Spec. No authentication required, per the
    /// registry. The registry declares the response as a bare <c>{"type": "object"}</c> - no
    /// documented shape beyond "it's the OpenAPI document itself" - so this returns the raw
    /// <see cref="JsonElement"/> rather than a typed model (SER-09).
    /// </summary>
    public Task<XResponse<JsonElement>> GetOpenApiSpecAsync(CancellationToken cancellationToken = default) =>
        _executor.SendAsync<JsonElement>(HttpMethod.Get, "2/openapi.json", cancellationToken);
}

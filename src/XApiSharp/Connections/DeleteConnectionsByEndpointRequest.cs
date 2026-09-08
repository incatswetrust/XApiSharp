using XApiSharp.Common;

namespace XApiSharp.Connections;

/// <summary>Request for <c>DELETE /2/connections/{endpoint_id}</c>.</summary>
public sealed class DeleteConnectionsByEndpointRequest
{
    public required XStreamEndpoint EndpointId { get; init; }
}

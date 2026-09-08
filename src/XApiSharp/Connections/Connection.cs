using System.Text.Json.Serialization;

namespace XApiSharp.Connections;

/// <summary>Modeled from the "Connection" schema.</summary>
public sealed class Connection
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("endpoint_name")]
    public string? EndpointName { get; init; }

    [JsonPropertyName("client_ip")]
    public string? ClientIp { get; init; }

    [JsonPropertyName("connected_at")]
    public DateTimeOffset? ConnectedAt { get; init; }

    [JsonPropertyName("disconnected_at")]
    public DateTimeOffset? DisconnectedAt { get; init; }

    [JsonPropertyName("disconnect_reason")]
    public string? DisconnectReason { get; init; }
}

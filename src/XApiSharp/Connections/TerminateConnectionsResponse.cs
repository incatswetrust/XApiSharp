using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Connections;

/// <summary>Shared response shape for all three terminate-connections operations
/// (DeleteConnectionsByUuidsResponse/DeleteAllConnectionsResponse/
/// DeleteConnectionsByEndpointResponse in the registry - identical structure).</summary>
public sealed class TerminateConnectionsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public TerminateConnectionsResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class TerminateConnectionsResponseData
{
    [JsonPropertyName("successful_kills")]
    public int SuccessfulKills { get; init; }

    [JsonPropertyName("failed_kills")]
    public int FailedKills { get; init; }

    [JsonPropertyName("results")]
    public IReadOnlyList<TerminateConnectionResult>? Results { get; init; }
}

public sealed class TerminateConnectionResult
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }
}

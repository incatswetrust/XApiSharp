using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>GET /2/broadcasts/scheduled/{id}</c>.</summary>
public sealed class GetScheduledBroadcastRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "GetScheduledBroadcastResponse" schema.</summary>
public sealed class GetScheduledBroadcastResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ScheduledBroadcast? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

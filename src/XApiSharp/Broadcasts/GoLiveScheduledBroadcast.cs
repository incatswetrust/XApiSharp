using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>POST /2/broadcasts/scheduled/{id}/live</c>.</summary>
public sealed class GoLiveScheduledBroadcastRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "GoLiveScheduledBroadcastResponse" schema.</summary>
public sealed class GoLiveScheduledBroadcastResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ScheduledBroadcast? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

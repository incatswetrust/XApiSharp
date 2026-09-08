using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>DELETE /2/broadcasts/scheduled/{id}</c>.</summary>
public sealed class DeleteScheduledBroadcastRequest
{
    public required string Id { get; init; }

    /// <summary>For a recurring schedule, whether to roll the recurrence forward to its next
    /// occurrence instead of cancelling the whole series - per the registry.</summary>
    public bool? RollForward { get; init; }
}

/// <summary>Modeled from the "DeleteScheduledBroadcastResponse" schema.</summary>
public sealed class DeleteScheduledBroadcastResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteScheduledBroadcastResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteScheduledBroadcastResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

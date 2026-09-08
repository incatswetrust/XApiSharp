using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.DirectMessages;

/// <summary>Request for <c>DELETE /2/dm_events/{event_id}</c>.</summary>
public sealed class DeleteDmEventRequest
{
    public required string EventId { get; init; }
}

/// <summary>Modeled from the "DeleteDirectMessagesEventsResponse" schema.</summary>
public sealed class DeleteDmEventResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteDmEventResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteDmEventResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

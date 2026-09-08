using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.DirectMessages;

/// <summary>Request for <c>GET /2/dm_events/{event_id}</c>.</summary>
public sealed class GetDmEventByIdRequest
{
    public required string EventId { get; init; }

    public IReadOnlyCollection<XDmEventField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }

    public IReadOnlyCollection<XMediaField>? MediaFields { get; init; }
}

/// <summary>Modeled from the "GetDirectMessagesEventsByIdResponse" schema.</summary>
public sealed class GetDmEventByIdResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DmEvent? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

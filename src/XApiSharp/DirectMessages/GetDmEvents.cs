using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.DirectMessages;

/// <summary>Shared request shape for the three DM-events-listing operations (all events, by
/// conversation, by participant) - identical parameter sets in the registry, differing only in
/// which conversation(s) they scope to.</summary>
public sealed class GetDmEventsRequest
{
    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XDmEventType>? EventTypes { get; init; }

    public IReadOnlyCollection<XDmEventField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }

    public IReadOnlyCollection<XMediaField>? MediaFields { get; init; }
}

/// <summary>Modeled from the "GetDirectMessagesEventsResponse" schema (shared by all three
/// operations - identical shape).</summary>
public sealed class GetDmEventsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<DmEvent>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

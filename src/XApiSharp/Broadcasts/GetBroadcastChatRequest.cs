using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>GET /2/broadcasts/{id}/chat</c>.</summary>
public sealed class GetBroadcastChatRequest
{
    public required string Id { get; init; }

    /// <summary>1-200, per the registry (default 100).</summary>
    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XBroadcastChatMessageField>? Fields { get; init; }

    /// <summary>Only <c>author_id</c> is meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }
}

/// <summary>Modeled from the "GetBroadcastChatResponse" schema.</summary>
public sealed class GetBroadcastChatResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<BroadcastChatMessage>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

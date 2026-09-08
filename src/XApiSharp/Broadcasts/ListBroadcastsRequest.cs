using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>GET /2/broadcasts</c>.</summary>
public sealed class ListBroadcastsRequest
{
    /// <summary>Up to 100 IDs, per the registry. Omit to list without an ID filter.</summary>
    public IReadOnlyCollection<string>? Ids { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XBroadcastField>? Fields { get; init; }
}

/// <summary>Modeled from the "ListBroadcastsResponse" schema.</summary>
public sealed class ListBroadcastsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Broadcast>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public ListBroadcastsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class ListBroadcastsMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>Request for <c>GET /2/tweets/search/stream/rules</c> - PAGE-10: <see cref="PaginationToken"/>
/// is opaque, obtained from a previous page's <c>meta.next_token</c>, never constructed or decoded.</summary>
public sealed class GetStreamRulesRequest
{
    /// <summary>Up to 1000 rule ids to look up; <see langword="null"/>/empty returns every rule.</summary>
    public IReadOnlyCollection<string>? Ids { get; init; }

    /// <summary>1-1000, default 1000 per the registry.</summary>
    public int? MaxResults { get; init; }

    public string? PaginationToken { get; init; }
}

/// <summary>Modeled from the "GetRulesResponse" schema.</summary>
public sealed class GetStreamRulesResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<StreamRule>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public GetStreamRulesMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class GetStreamRulesMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

public sealed class StreamRule
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

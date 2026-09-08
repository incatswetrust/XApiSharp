using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.News;

/// <summary>Request for <c>GET /2/news/search</c>. Not paginated - the registry declares only a
/// <c>result_count</c> hint in <c>meta</c>, no continuation token.</summary>
public sealed class SearchNewsRequest
{
    /// <summary>1-2048 characters, per the registry.</summary>
    public required string Query { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>1-720, per the registry (default 168 = one week).</summary>
    public int? MaxAgeHours { get; init; }

    public IReadOnlyCollection<XNewsField>? Fields { get; init; }
}

/// <summary>Modeled from the "SearchNewsResponse" schema.</summary>
public sealed class SearchNewsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<News>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public SearchNewsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class SearchNewsMeta
{
    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

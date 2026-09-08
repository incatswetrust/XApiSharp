using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Communities;

/// <summary>Request for <c>GET /2/communities/search</c>. The registry accepts both
/// <c>next_token</c> and <c>pagination_token</c> as aliases (like the Posts search operations) -
/// <c>next_token</c> is used here since that's what the response's own <c>meta</c> echoes back.</summary>
public sealed class SearchCommunitiesRequest
{
    /// <summary>1-4096 characters, per the registry.</summary>
    public required string Query { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>.</summary>
    public string? NextToken { get; init; }

    public IReadOnlyCollection<XCommunityField>? Fields { get; init; }
}

/// <summary>Modeled from the "SearchCommunitiesResponse" schema.</summary>
public sealed class SearchCommunitiesResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Community>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public SearchCommunitiesMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

/// <summary>Not <see cref="XPageMeta"/> - only <c>next_token</c> in this operation's schema, no
/// <c>previous_token</c>/<c>result_count</c> (PAGE-02).</summary>
public sealed class SearchCommunitiesMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Shared request shape for <c>GET /2/tweets/search/recent</c> and
/// <c>GET /2/tweets/search/all</c> - identical parameter sets (search/all's <c>MaxResults</c>
/// ceiling is higher per the registry - 500 vs 100 - not enforced client-side per API-10).</summary>
public sealed class SearchPostsRequest
{
    /// <summary>1-4096 characters, per the registry.</summary>
    public required string Query { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>. The
    /// registry also accepts this as `pagination_token` (an alias) - `next_token` is used here
    /// since that's the field name the response's own `meta` echoes back.</summary>
    public string? NextToken { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public string? SinceId { get; init; }

    public string? UntilId { get; init; }

    public XSortOrder? SortOrder { get; init; }

    public XPostFieldSelection? Fields { get; init; }
}

/// <summary>Modeled from the "SearchPostsRecentResponse"/"SearchPostsAllResponse" schemas - both
/// identical in shape. Not <see cref="XIncludes"/> alone at the top - the response also carries a
/// richer <see cref="SearchPostsMeta"/> than <see cref="XPageMeta"/> (adds
/// <c>newest_id</c>/<c>oldest_id</c> - PAGE-02).</summary>
public sealed class SearchPostsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Post>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public SearchPostsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class SearchPostsMeta
{
    [JsonPropertyName("newest_id")]
    public string? NewestId { get; init; }

    [JsonPropertyName("oldest_id")]
    public string? OldestId { get; init; }

    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("previous_token")]
    public string? PreviousToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

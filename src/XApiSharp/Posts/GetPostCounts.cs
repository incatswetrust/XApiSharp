using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Shared request shape for <c>GET /2/tweets/counts/recent</c> and
/// <c>GET /2/tweets/counts/all</c> - identical parameter sets, differing only in access level and
/// retention window (per the registry: recent covers ~7 days, all covers the full archive; not
/// enforced client-side per API-10).</summary>
public sealed class GetPostCountsRequest
{
    /// <summary>1-4096 characters, per the registry.</summary>
    public required string Query { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public string? SinceId { get; init; }

    public string? UntilId { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>.</summary>
    public string? NextToken { get; init; }

    public XCountGranularity? Granularity { get; init; }
}

/// <summary>Modeled from the "GetPostsCountsRecentResponse"/"GetPostsCountsAllResponse" schemas -
/// both identical in shape.</summary>
public sealed class GetPostCountsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<PostCountBucket>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public PostCountsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class PostCountBucket
{
    [JsonPropertyName("start")]
    public required string Start { get; init; }

    [JsonPropertyName("end")]
    public required string End { get; init; }

    [JsonPropertyName("post_count")]
    public long PostCount { get; init; }
}

/// <summary>Not <see cref="XPageMeta"/> - this operation's meta shape is `next_token` +
/// `total_post_count`, no `previous_token`/`result_count` (PAGE-02).</summary>
public sealed class PostCountsMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("total_post_count")]
    public long? TotalPostCount { get; init; }
}

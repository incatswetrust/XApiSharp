using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Shared request shape for the Posts-family operations that page over a list of
/// <see cref="Post"/> related to a given Post (quote posts, reposts). <see cref="Exclude"/> is
/// only meaningful for quote posts, per the registry - left unset on reposts is a no-op.</summary>
public sealed class PostsPageRequest
{
    public required string PostId { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XPostExclusionFilter>? Exclude { get; init; }

    public XPostFieldSelection? Fields { get; init; }
}

/// <summary>Shared page shape for <see cref="PostsPageRequest"/>-based operations.</summary>
public sealed class PostsPageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Post>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

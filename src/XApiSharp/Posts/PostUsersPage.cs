using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.Posts;

/// <summary>Shared request shape for the Posts-family operations that page over a list of
/// <see cref="User"/> for a given Post (liking users, reposted-by).</summary>
public sealed class PostUsersPageRequest
{
    public required string PostId { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    /// <summary>Only <c>affiliation</c>/<c>most_recent_post_id</c>/<c>pinned_post_id</c> are
    /// meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }
}

/// <summary>Shared page shape for <see cref="PostUsersPageRequest"/>-based operations.</summary>
public sealed class PostUsersPageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<User>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

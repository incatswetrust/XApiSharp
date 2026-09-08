using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>
/// Shared request shape for the Users-family operations that page over a list of
/// <see cref="Common.Post"/> (own posts, mentions, timeline, liked posts, reposts-of-me,
/// bookmarks). Not every field applies to every operation - <c>StartTime</c>/<c>EndTime</c>/
/// <c>SinceId</c>/<c>UntilId</c>/<c>Exclude</c> are only meaningful for
/// <see cref="UsersClient.GetPostsAsync"/>/<see cref="UsersClient.GetMentionsAsync"/>/
/// <see cref="UsersClient.GetTimelineAsync"/> per the registry; leaving them unset on an operation
/// that doesn't support them is a no-op (they're simply not sent), per API-10 - the SDK doesn't
/// locally re-derive which subset each operation accepts.
/// </summary>
public sealed class GetPostsPageRequest
{
    public required string UserId { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public string? SinceId { get; init; }

    public string? UntilId { get; init; }

    public IReadOnlyCollection<XPostExclusionFilter>? Exclude { get; init; }

    public IReadOnlyCollection<XPostField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }
}

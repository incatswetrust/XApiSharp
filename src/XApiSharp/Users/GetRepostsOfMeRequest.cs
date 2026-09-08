using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users/reposts_of_me</c>. No <c>id</c> path parameter - this
/// operates on the authenticated user, like <see cref="GetMyUserRequest"/>. Narrower parameter set
/// than <see cref="GetPostsPageRequest"/> (no start/end time, since/until ID, or exclude filter,
/// per the registry).</summary>
public sealed class GetRepostsOfMeRequest
{
    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XPostField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }
}

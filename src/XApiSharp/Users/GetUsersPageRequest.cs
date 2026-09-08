using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Shared request shape for the Users-family <c>GET /2/users/{id}/...</c> operations that
/// page over a list of <see cref="User"/> keyed by the same <c>pagination_token</c>/
/// <c>max_results</c> pair (followers, following, blocking, muting, affiliates). Search uses a
/// different token parameter name (PAGE-02) and has no path <c>id</c>, so it gets its own
/// <see cref="SearchUsersRequest"/> instead of reusing this type.</summary>
public sealed class GetUsersPageRequest
{
    public required string UserId { get; init; }

    /// <summary>1-1000, per the registry. <see langword="null"/> uses the endpoint's own
    /// default.</summary>
    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>,
    /// never constructed or decoded by the caller.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

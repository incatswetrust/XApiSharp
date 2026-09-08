using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Shared request shape for the paginated Users-family operations that page over a list
/// of <see cref="XList"/> (followed lists, list memberships, owned lists). Pinned lists is a
/// separate, non-paginated operation - see <see cref="GetPinnedListsRequest"/> - the registry
/// doesn't expose <c>max_results</c>/<c>pagination_token</c> for it at all (X caps pinned lists at
/// a small fixed count).</summary>
public sealed class GetListsPageRequest
{
    public required string UserId { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XListField>? Fields { get; init; }

    /// <summary>Only <see cref="XExpansion.OwnerId"/> is meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }
}

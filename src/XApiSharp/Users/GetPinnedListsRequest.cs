using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users/{id}/pinned_lists</c>. Not paginated - the registry
/// exposes no <c>max_results</c>/<c>pagination_token</c> for this operation.</summary>
public sealed class GetPinnedListsRequest
{
    public required string UserId { get; init; }

    public IReadOnlyCollection<XListField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }
}

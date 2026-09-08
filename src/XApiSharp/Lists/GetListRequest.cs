using XApiSharp.Common;

namespace XApiSharp.Lists;

/// <summary>Request for <c>GET /2/lists/{id}</c>.</summary>
public sealed class GetListRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XListField>? Fields { get; init; }

    /// <summary>Only <see cref="XExpansion.OwnerId"/> is meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }
}

using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users</c> - up to 100 IDs per call, per the registry.</summary>
public sealed class GetUsersByIdsRequest
{
    public required IReadOnlyCollection<string> Ids { get; init; }

    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

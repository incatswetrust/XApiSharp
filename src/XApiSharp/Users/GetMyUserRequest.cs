using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users/me</c>. No required parameters - the identity comes from
/// the authenticated OAuth 2.0 user context.</summary>
public sealed class GetMyUserRequest
{
    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users/by</c> - up to 100 usernames per call, per the registry.</summary>
public sealed class GetUsersByUsernamesRequest
{
    public required IReadOnlyCollection<string> Usernames { get; init; }

    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

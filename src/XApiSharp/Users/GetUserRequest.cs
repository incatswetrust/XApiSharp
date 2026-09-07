namespace XApiSharp.Users;

/// <summary>
/// Request for <c>GET /2/users/{id}</c>. <c>fields</c>/<c>expansions</c> parameters (SER-05)
/// are added when the full Users family lands in E4 - this vertical slice covers the plain
/// by-ID lookup only.
/// </summary>
public sealed class GetUserRequest
{
    public required string Id { get; init; }
}

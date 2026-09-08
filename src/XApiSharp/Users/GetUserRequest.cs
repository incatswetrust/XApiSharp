using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users/{id}</c>.</summary>
public sealed class GetUserRequest
{
    public required string Id { get; init; }

    /// <summary>SER-05: <c>user.fields</c> - which optional <see cref="User"/> fields to include.</summary>
    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    /// <summary>SER-05: <c>expansions</c> - which related objects to embed in the response's
    /// <c>Includes</c>.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    /// <summary>SER-05: <c>post.fields</c> - which optional <see cref="Common.Post"/> fields to
    /// include on any expanded post (<c>pinned_post_id</c>/<c>most_recent_post_id</c>).</summary>
    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

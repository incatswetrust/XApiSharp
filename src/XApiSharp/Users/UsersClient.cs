using XApiSharp.Transport;

namespace XApiSharp.Users;

/// <summary>Typed methods for the Users family. Only <see cref="GetByIdAsync"/> exists in the
/// E2 vertical slice; the rest of the family (36 operations per the registry) lands in E4.</summary>
public sealed class UsersClient
{
    private readonly RequestExecutor _executor;

    internal UsersClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/users/{id}</c> - Get Users by ID. Requires app-only bearer, OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c>), or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetUserResponse>> GetByIdAsync(GetUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<GetUserResponse>(
            HttpMethod.Get,
            $"2/users/{Uri.EscapeDataString(request.Id)}",
            cancellationToken);
    }
}

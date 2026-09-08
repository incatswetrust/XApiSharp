using XApiSharp.Transport;

namespace XApiSharp.Account;

/// <summary>Typed methods for the Account family (2 operations per the registry) - developer
/// account management, distinct from <see cref="Users.UsersClient"/> (end-user X accounts).</summary>
public sealed class AccountClient
{
    private readonly RequestExecutor _executor;

    internal AccountClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/account</c> - Get developer account. Requires OAuth 2.0 <c>developer.read</c>.
    /// </summary>
    public Task<XResponse<GetDeveloperAccountResponse>> GetAsync(CancellationToken cancellationToken = default) =>
        _executor.SendAsync<GetDeveloperAccountResponse>(HttpMethod.Get, "2/account", cancellationToken);

    /// <summary>
    /// <c>POST /2/account</c> - Ensure developer account (creates it if it doesn't already
    /// exist). Requires OAuth 2.0 <c>developer.write</c>.
    /// </summary>
    public Task<XResponse<EnsureAccountResponse>> EnsureAsync(EnsureAccountRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EnsureAccountRequest();

        return _executor.SendAsync<EnsureAccountResponse>(
            HttpMethod.Post,
            "2/account",
            new EnsureAccountBody { Metadata = request.Metadata },
            queryParameters: null,
            cancellationToken);
    }
}

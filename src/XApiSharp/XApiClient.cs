using XApiSharp.Authentication;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp;

/// <summary>
/// Entry point for the SDK. Groups endpoint clients by entity (spec section 8.1). The caller owns
/// the <see cref="HttpClient"/>'s lifetime (HTTP-01) - this type never disposes it.
/// </summary>
public sealed class XApiClient
{
    public XApiClient(HttpClient httpClient, IXAuthenticationProvider authenticationProvider, XClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(authenticationProvider);

        var resolvedOptions = options ?? new XClientOptions();
        var executor = new RequestExecutor(httpClient, authenticationProvider, resolvedOptions);

        Users = new UsersClient(executor);
    }

    public UsersClient Users { get; }
}

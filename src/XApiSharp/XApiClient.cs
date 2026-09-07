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
    /// <param name="httpClient">Externally owned - never disposed by this type (HTTP-01).</param>
    /// <param name="authenticationProvider">Prepares auth for every outgoing request.</param>
    /// <param name="options">Defaults to <c>new XClientOptions()</c> when omitted.</param>
    /// <param name="timeProvider">Defaults to <see cref="TimeProvider.System"/>. Inject a fake
    /// provider in tests to control <see cref="XClientOptions.OperationTimeout"/>/
    /// <see cref="XClientOptions.AttemptTimeout"/> deterministically, without real sleeps.</param>
    public XApiClient(HttpClient httpClient, IXAuthenticationProvider authenticationProvider, XClientOptions? options = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(authenticationProvider);

        var resolvedOptions = options ?? new XClientOptions();
        var resolvedTimeProvider = timeProvider ?? TimeProvider.System;
        var executor = new RequestExecutor(httpClient, authenticationProvider, resolvedOptions, resolvedTimeProvider);

        Users = new UsersClient(executor);
    }

    public UsersClient Users { get; }
}

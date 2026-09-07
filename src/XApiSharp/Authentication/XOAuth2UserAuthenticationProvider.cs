using System.Net.Http.Headers;
using XApiSharp.Errors;

namespace XApiSharp.Authentication;

/// <summary>
/// Attaches an OAuth 2.0 user access token to every request, refreshing it via
/// <see cref="XOAuth2Client"/> when it's expired or about to expire. One user/app context per
/// instance - construct a separate instance (and, in DI, resolve one per user) rather than
/// sharing this across users, so tokens never cross contexts (spec section 19.2 isolation
/// requirement).
/// </summary>
public sealed class XOAuth2UserAuthenticationProvider : IXRefreshableAuthenticationProvider, IDisposable
{
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(30);

    private readonly XOAuth2Client _oauth2Client;
    private readonly IXOAuth2TokenStore _store;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public XOAuth2UserAuthenticationProvider(XOAuth2Client oauth2Client, IXOAuth2TokenStore? store = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(oauth2Client);
        _oauth2Client = oauth2Client;
        _store = store ?? new XInMemoryOAuth2TokenStore();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Dispose() => _refreshGate.Dispose();

    /// <summary>Seeds the store after the user completes the browser flow (
    /// <see cref="XOAuth2Client.CompleteAuthorizationAsync"/> / <c>ExchangeCodeAsync</c>). Call
    /// this once per new authorization; refreshes update the store on their own afterwards.</summary>
    public async Task SetInitialTokenAsync(XOAuth2TokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tokenResponse);
        var current = await _store.GetAsync(cancellationToken).ConfigureAwait(false);
        var ok = await _store.TrySetAsync(tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresAtUtc, current?.Version, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            throw new InvalidOperationException("Token store version conflict while setting the initial token - another writer updated it concurrently.");
        }
    }

    public async ValueTask PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetValidTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
    }

    /// <summary>Forces a refresh regardless of the cached token's stated expiry - used by
    /// <see cref="Transport.RequestExecutor"/> after a 401 (the token may have been revoked
    /// server-side before its stated expiry, so the normal "still looks valid" check must not
    /// suppress this one).</summary>
    public async Task ForceRefreshAsync(CancellationToken cancellationToken)
    {
        var current = await _store.GetAsync(cancellationToken).ConfigureAwait(false);
        await RefreshAsync(current?.Version, cancellationToken).ConfigureAwait(false);
    }

    private async Task<XOAuth2StoredToken> GetValidTokenAsync(CancellationToken cancellationToken)
    {
        var current = await _store.GetAsync(cancellationToken).ConfigureAwait(false);
        if (current is null)
        {
            throw new XAuthenticationException(
                "No OAuth 2.0 token is available yet - complete the authorization flow and call SetInitialTokenAsync first.");
        }

        if (current.ExpiresAtUtc > _timeProvider.GetUtcNow() + ExpiryBuffer)
        {
            return current;
        }

        return await RefreshAsync(current.Version, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// AUTH-07: single-flight - concurrent callers block on the same gate and share one refresh
    /// instead of each starting their own. AUTH-11: once a caller is the one performing the
    /// refresh, it proceeds to completion on <see cref="CancellationToken.None"/> - one caller's
    /// cancellation (that caller's own, or another's while waiting for the gate) never aborts
    /// the shared result the rest are waiting for. AUTH-10: a refresh failure clears the stored
    /// refresh token so the *next* call fails fast with a clear re-authorize error instead of
    /// attempting another network refresh.
    /// </summary>
    /// <param name="observedVersion">The store's version as last seen by the caller, before it
    /// decided a refresh was needed and asked for the gate. Used (not the token's expiry) to
    /// detect "someone else already handled this" - expiry can't do that for a *forced* refresh,
    /// since the whole point of forcing is that the token looks unexpired but was rejected by
    /// the server anyway.</param>
    /// <param name="cancellationToken">Only observed while waiting for the gate - once this
    /// caller becomes the one performing the refresh, the network call itself is not cancellable
    /// by it (AUTH-11).</param>
    private async Task<XOAuth2StoredToken> RefreshAsync(string? observedVersion, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var current = await _store.GetAsync(CancellationToken.None).ConfigureAwait(false);

            // The store moved on from what we observed before entering the gate - someone else
            // already refreshed (or force-refreshed) while we were waiting. Use their result
            // instead of making a redundant network call.
            if (current is { } c && !string.Equals(c.Version, observedVersion, StringComparison.Ordinal))
            {
                return c;
            }

            if (current?.RefreshToken is not { } refreshToken)
            {
                throw new XAuthenticationException(
                    "No refresh token is available to obtain a new access token - restart the authorization flow.");
            }

            XOAuth2TokenResponse refreshed;
            try
            {
                refreshed = await _oauth2Client.RefreshTokenAsync(refreshToken, CancellationToken.None).ConfigureAwait(false);
            }
            catch (XAuthenticationException)
            {
                // AUTH-10: don't leave a still-present (but now known-dead) refresh token behind
                // for the next call to try again with - clear it so that call fails fast instead.
                await _store.TrySetAsync(current.AccessToken, refreshToken: null, current.ExpiresAtUtc, current.Version, CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            await _store.TrySetAsync(refreshed.AccessToken, refreshed.RefreshToken, refreshed.ExpiresAtUtc, current.Version, CancellationToken.None).ConfigureAwait(false);

            // Re-read rather than reconstructing locally: on success this picks up the store's
            // real new version; on a CAS conflict (a concurrent external writer per AUTH-09) it
            // returns whatever that writer actually wrote instead of silently discarding it.
            var latest = await _store.GetAsync(CancellationToken.None).ConfigureAwait(false);
            return latest ?? new XOAuth2StoredToken
            {
                AccessToken = refreshed.AccessToken,
                RefreshToken = refreshed.RefreshToken,
                ExpiresAtUtc = refreshed.ExpiresAtUtc,
                Version = current.Version,
            };
        }
        finally
        {
            _refreshGate.Release();
        }
    }
}

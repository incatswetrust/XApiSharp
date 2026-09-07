using System.Globalization;

namespace XApiSharp.Authentication;

/// <summary>Default, in-process-only token store (AUTH-12: the SDK never persists to disk on
/// its own). Not shared across processes - see <see cref="IXOAuth2TokenStore"/> remarks.</summary>
public sealed class XInMemoryOAuth2TokenStore : IXOAuth2TokenStore
{
    private readonly Lock _lock = new();
    private XOAuth2StoredToken? _current;
    private long _versionCounter;

    public Task<XOAuth2StoredToken?> GetAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_current);
        }
    }

    public Task<bool> TrySetAsync(string accessToken, string? refreshToken, DateTimeOffset expiresAtUtc, string? expectedVersion, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!string.Equals(_current?.Version, expectedVersion, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _versionCounter++;
            _current = new XOAuth2StoredToken
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAtUtc = expiresAtUtc,
                Version = _versionCounter.ToString(CultureInfo.InvariantCulture),
            };

            return Task.FromResult(true);
        }
    }
}

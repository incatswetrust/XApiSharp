namespace XApiSharp.Authentication;

/// <summary>
/// External storage for an OAuth 2.0 user token set (spec AUTH-08). The default
/// (<see cref="XInMemoryOAuth2TokenStore"/>) is in-process only - the SDK never writes tokens to
/// disk automatically (AUTH-12); a durable implementation is the consumer's own, backed by
/// whatever secret-management mechanism their application already uses.
/// </summary>
public interface IXOAuth2TokenStore
{
    Task<XOAuth2StoredToken?> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Compare-and-swap write (AUTH-08): succeeds only if the store's current version still
    /// equals <paramref name="expectedVersion"/> (null means "expect no value present yet").
    /// Returns false on a version conflict instead of overwriting - callers re-read and decide
    /// what to do rather than silently losing a concurrent writer's update. A single in-process
    /// <see cref="XOAuth2UserAuthenticationProvider"/> already serializes its own writes; this
    /// CAS contract is what makes a *distributed* store (multiple processes) safe on top of that
    /// (AUTH-09) - the SDK's own in-process lock is never a substitute for cross-process
    /// coordination.
    /// </summary>
    Task<bool> TrySetAsync(string accessToken, string? refreshToken, DateTimeOffset expiresAtUtc, string? expectedVersion, CancellationToken cancellationToken);
}

public sealed class XOAuth2StoredToken
{
    public required string AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public required DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>Opaque CAS version - compare for equality only, never parse or order it.</summary>
    public required string Version { get; init; }
}

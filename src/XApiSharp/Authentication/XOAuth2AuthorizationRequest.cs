namespace XApiSharp.Authentication;

/// <summary>
/// State an Authorization Code + PKCE flow needs to survive the round trip to the browser and
/// back (spec AUTH-02). The SDK does not persist this itself (it doesn't own a session store,
/// and a web app may handle the callback on a different process/instance) - the caller is
/// responsible for storing it (e.g. in their own encrypted session) keyed by <see cref="State"/>,
/// and for treating it as single-use: delete/invalidate it after the first
/// <see cref="XOAuth2Client.CompleteAuthorizationAsync"/> call, successful or not.
/// </summary>
public sealed class XOAuth2AuthorizationRequest
{
    public required Uri AuthorizationUrl { get; init; }

    public required string State { get; init; }

    public required string CodeVerifier { get; init; }

    public required Uri RedirectUri { get; init; }

    /// <summary>After this time, <see cref="XOAuth2Client.CompleteAuthorizationAsync"/> refuses
    /// the session and the flow must restart (spec AUTH-02).</summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }
}

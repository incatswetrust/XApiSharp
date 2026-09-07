namespace XApiSharp.Authentication;

/// <summary>Result of a successful OAuth 2.0 token exchange or refresh.</summary>
public sealed class XOAuth2TokenResponse
{
    public required string AccessToken { get; init; }

    /// <summary>Present only if the authorization request included the <c>offline.access</c>
    /// scope (spec AUTH-05).</summary>
    public string? RefreshToken { get; init; }

    public required string TokenType { get; init; }

    /// <summary>Computed from the server's <c>expires_in</c> at the moment of the response
    /// (spec AUTH-06) - never a hardcoded duration.</summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = [];
}

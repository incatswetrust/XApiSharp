using XApiSharp.Authentication;

namespace XApiSharp.Extensions.DependencyInjection;

/// <summary>
/// Builds an <see cref="XApiClient"/> bound to one application user's OAuth 2.0 context (spec
/// 18.1: "a factory of clients bound to a specific authorization context"). Registered as a
/// singleton by <c>AddXApiSharpMultiUser</c> - it is safe to be one, because it captures no
/// per-user token itself; each call builds a fresh <see cref="XOAuth2UserAuthenticationProvider"/>
/// wired to that user's own <see cref="Authentication.IXOAuth2TokenStore"/>
/// (via <see cref="IXOAuth2TokenStoreFactory"/>). Never resolve or cache a per-user
/// <see cref="XApiClient"/> as a singleton yourself - a user's token must never be captured by a
/// shared, application-lifetime instance (spec 18.1).
/// </summary>
public interface IXApiUserClientFactory
{
    /// <summary>The shared <see cref="XOAuth2Client"/> for this application's OAuth 2.0 app
    /// registration - use it to drive the authorization-code/PKCE flow (creating an
    /// authorization request, completing it) before a client can be built for a new user.</summary>
    XOAuth2Client OAuth2Client { get; }

    /// <summary>
    /// Gets or creates the <see cref="XOAuth2UserAuthenticationProvider"/> for <paramref name="userId"/>
    /// - call <see cref="XOAuth2UserAuthenticationProvider.SetInitialTokenAsync"/> on it once,
    /// right after completing that user's authorization flow.
    /// </summary>
    XOAuth2UserAuthenticationProvider GetAuthenticationProvider(string userId);

    /// <summary>
    /// Builds a fresh <see cref="XApiClient"/> for <paramref name="userId"/>, using that user's
    /// authentication provider (see <see cref="GetAuthenticationProvider"/>) and a pooled
    /// <see cref="HttpClient"/> from <see cref="IHttpClientFactory"/>. Cheap to call repeatedly -
    /// <see cref="XApiClient"/> itself is a lightweight wrapper, so this does not need to be
    /// cached by the caller.
    /// </summary>
    XApiClient CreateForUser(string userId);
}

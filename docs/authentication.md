# Authentication

Every `XApiClient` takes one `IXAuthenticationProvider` (`XApiSharp.Authentication`), which
prepares the `Authorization` header on each outgoing request. There are four implementations,
matching the four ways the X API v2 accepts credentials; pick the narrowest one your app actually
needs.

## App-only bearer token

The simplest mode - read-only, no per-user context. Use `BearerTokenAuthenticationProvider` if
you already have a bearer token (from the developer dashboard, or issued by your own process):

```csharp
IXAuthenticationProvider auth = new BearerTokenAuthenticationProvider(bearerToken);
var client = new XApiClient(httpClient, auth);
```

This provider does not acquire, cache, or refresh a token itself - "passing an already-obtained
token" is deliberately the simple, self-contained case (spec section 10.1). If you'd rather have
the SDK obtain the token from your consumer key/secret (`POST /oauth2/token`,
`grant_type=client_credentials`) and cache/refresh it for you, use `XAppOnlyAuthenticationProvider`
instead:

```csharp
using var auth = new XAppOnlyAuthenticationProvider(httpClient, consumerKey, consumerSecret);
var client = new XApiClient(httpClient, auth);
// First request triggers the fetch; later requests reuse the cached token. Concurrent first
// requests trigger exactly one fetch (single-flight), not one per caller.
```

Call `auth.RefreshAsync(cancellationToken)` yourself if you need to force a new token (e.g. after
revoking access elsewhere) - app-only tokens don't expire under normal operation per X's own docs,
so there's no automatic refresh-on-401 loop for this mode.

## OAuth 2.0 Authorization Code + PKCE (per-user)

Three pieces: `XOAuth2Client` drives the browser-redirect flow and token exchange;
`XOAuth2UserAuthenticationProvider` wraps a token and refreshes it on demand; `IXOAuth2TokenStore`
is where the token set actually lives (defaults to `XInMemoryOAuth2TokenStore` - in-process only,
never written to disk automatically). See `samples/` for a full ASP.NET Core walkthrough.

```csharp
var oauth2Client = new XOAuth2Client(httpClient, clientId, clientSecret /* omit for a public client */);

// 1. Send the user's browser here. offline.access is required to get a refresh token back -
//    it is never added implicitly (request exactly the scopes you need).
var authRequest = oauth2Client.CreateAuthorizationRequest(
    redirectUri: new Uri("https://your-app.example/callback"),
    scopes: ["tweet.read", "users.read", "offline.access"]);
// Persist authRequest (State/CodeVerifier/ExpiresAtUtc) keyed to the user's session until the
// callback arrives - it's short-lived, not a long-term credential.

// 2. On the callback request:
var tokens = await oauth2Client.CompleteAuthorizationAsync(authRequest, callbackUri, cancellationToken);

// 3. Wrap it in a provider and seed the store once:
var auth = new XOAuth2UserAuthenticationProvider(oauth2Client);
await auth.SetInitialTokenAsync(tokens, cancellationToken);
var client = new XApiClient(httpClient, auth);
```

`CompleteAuthorizationAsync` validates the callback against the original request (session not
expired, no OAuth error, `state` matches, callback origin/path matches the original
`redirect_uri`) before exchanging the code - a mismatch throws rather than silently trusting the
callback. After the initial exchange, `XOAuth2UserAuthenticationProvider` refreshes the token on
its own (once, single-flight, 30 seconds before expiry or on a 401) - you never call refresh
yourself in normal operation.

**One provider instance per user.** `XOAuth2UserAuthenticationProvider` holds exactly one user's
token context; sharing one instance across users would cross tokens between them. In DI, resolve
one per user/request scope - see `docs/di-and-multi-user.md`.

### Token storage

The default `XInMemoryOAuth2TokenStore` is process-local and lost on restart - fine for a quick
script, not for a real multi-instance or restart-surviving app. Implement `IXOAuth2TokenStore`
against your own secret store (a database, a secrets manager, whatever your app already uses):

```csharp
public interface IXOAuth2TokenStore
{
    Task<XOAuth2StoredToken?> GetAsync(CancellationToken cancellationToken);
    Task<bool> TrySetAsync(string accessToken, string? refreshToken, DateTimeOffset expiresAtUtc, string? expectedVersion, CancellationToken cancellationToken);
}
```

`TrySetAsync` is a compare-and-swap write: it succeeds only if the store's current version still
matches `expectedVersion` (`null` means "expect nothing stored yet"), returning `false` on a
conflict instead of silently overwriting a concurrent writer's update. This is what makes a store
shared across multiple processes/instances safe - the SDK's own in-process lock only protects a
single `XOAuth2UserAuthenticationProvider` instance, not concurrent writers across processes.

## OAuth 1.0a

For endpoints/features that require the older signing scheme (some media and DM operations still
do):

```csharp
IXAuthenticationProvider auth = new XOAuth1AuthenticationProvider(consumerKey, consumerSecret, accessToken, accessTokenSecret);
var client = new XApiClient(httpClient, auth);
```

Signs every request with HMAC-SHA1 per the
[OAuth 1.0a spec](https://docs.x.com/fundamentals/authentication/oauth-1-0a/creating-a-signature) -
computed fresh per request (method, URL, and body all feed the signature), never by mutating a
shared `HttpClient.DefaultRequestHeaders.Authorization`. One instance per app+user context, same
isolation reasoning as the OAuth 2.0 provider.

## Choosing scopes

Request the narrowest set of [scopes](https://docs.x.com/x-api/fundamentals/authentication/oauth-2-0/scopes)
each operation you call actually needs - every typed method's XML doc comment states which
scope(s)/auth mode it requires (visible via IntelliSense), sourced from the operation's own
`security` requirement in the OpenAPI contract, not inferred or guessed.

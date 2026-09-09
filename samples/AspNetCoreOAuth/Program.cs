// ASP.NET Core OAuth 2.0 Authorization Code + PKCE sample (spec section 20 / section 10.2 /
// section 18.1's "multi-user ASP.NET Core app" DI sample). Demonstrates the full three-request
// flow (/login -> X's own authorize page -> /callback) using AddXApiSharpMultiUser and
// IXApiUserClientFactory - see docs/authentication.md and docs/di-and-multi-user.md for the
// concepts this sample exercises.
// This sample keeps pending-authorization state in the built-in session (in-memory,
// single-process, lost on restart) purely to correlate a browser back to its own /login call.
// Actual tokens live in the DI package's default IXOAuth2TokenStoreFactory (also in-memory) -
// a real multi-instance app replaces that registration with a durable one (see
// docs/di-and-multi-user.md); this sample's session usage never needs to change either way,
// since the session only ever holds a userId, never a token.
// Requires X_CLIENT_ID (and, for a confidential/server-side app, X_CLIENT_SECRET) - see
// docs/getting-started.md for how to obtain them. This sample is not part of automated CI test
// execution - CI only verifies it compiles.
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Extensions.DependencyInjection;

var clientId = Environment.GetEnvironmentVariable("X_CLIENT_ID");
if (string.IsNullOrWhiteSpace(clientId))
{
    Console.Error.WriteLine("Set the X_CLIENT_ID environment variable to the app's OAuth 2.0 Client ID and try again.");
    return 1;
}

var clientSecret = Environment.GetEnvironmentVariable("X_CLIENT_SECRET");
var scopes = new[] { "tweet.read", "users.read", "offline.access" };

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddXApiSharpMultiUser(clientId, clientSecret);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => options.Cookie.HttpOnly = true);

var app = builder.Build();
app.UseSession();

app.MapGet("/", () => Results.Content(
    """<a href="/login">Sign in with X</a>""",
    "text/html"));

app.MapGet("/login", (HttpContext context, IXApiUserClientFactory clientFactory) =>
{
    var redirectUri = new Uri($"{context.Request.Scheme}://{context.Request.Host}/callback");
    var authRequest = clientFactory.OAuth2Client.CreateAuthorizationRequest(redirectUri, scopes);

    // Single-use, short-lived - the SDK doesn't persist this itself (it doesn't own a session
    // store), so the caller stores it keyed to the browser session until the callback arrives.
    context.Session.SetString("pending_auth", JsonSerializer.Serialize(authRequest));

    return Results.Redirect(authRequest.AuthorizationUrl.ToString());
});

app.MapGet("/callback", async (HttpContext context, IXApiUserClientFactory clientFactory, CancellationToken cancellationToken) =>
{
    var pendingJson = context.Session.GetString("pending_auth");
    if (pendingJson is null)
    {
        return Results.BadRequest("No authorization in progress for this session - start at /login.");
    }

    context.Session.Remove("pending_auth");

    var authRequest = JsonSerializer.Deserialize<XOAuth2AuthorizationRequest>(pendingJson)
        ?? throw new InvalidOperationException("Failed to deserialize the pending authorization request.");

    var callbackUri = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");

    XOAuth2TokenResponse tokens;
    try
    {
        tokens = await clientFactory.OAuth2Client.CompleteAuthorizationAsync(authRequest, callbackUri, cancellationToken);
    }
    catch (XAuthenticationException ex)
    {
        return Results.BadRequest($"Authorization failed: {ex.Message}");
    }

    // This sample's own notion of "user ID" is just a fresh random string, standing in for
    // whatever your app's real user identity/session system provides - the DI factory only
    // needs a stable key to look up this user's token store by.
    var userId = context.Session.Id;
    var authProvider = clientFactory.GetAuthenticationProvider(userId);
    await authProvider.SetInitialTokenAsync(tokens, cancellationToken);

    context.Session.SetString("user_id", userId);

    return Results.Redirect("/me");
});

app.MapGet("/me", async (HttpContext context, IXApiUserClientFactory clientFactory, CancellationToken cancellationToken) =>
{
    var userId = context.Session.GetString("user_id");
    if (userId is null)
    {
        return Results.Redirect("/login");
    }

    // Cheap to build per request - XApiClient itself is a lightweight wrapper reusing a pooled
    // HttpClient and this user's already-authorized provider (spec 18.1: never cache/singleton
    // a per-user client yourself).
    var client = clientFactory.CreateForUser(userId);
    var response = await client.Users.GetMeAsync(cancellationToken: cancellationToken);

    return Results.Text($"Signed in as @{response.Body?.Data?.Username} ({response.Body?.Data?.Name})");
});

app.Run();
return 0;

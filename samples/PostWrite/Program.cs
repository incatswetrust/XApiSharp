// User post sample with an explicit write trigger (spec section 20). Creating a Post is a real,
// visible, hard-to-undo action against the authenticated user's account, so this sample refuses
// to run unless X_CONFIRM_WRITE=yes is set in addition to the credentials - a plain "have a
// token" is not treated as consent to actually post.
// Requires a user access token with tweet.write scope: set X_USER_ACCESS_TOKEN before running
// (obtained via the OAuth 2.0 PKCE flow - see the AspNetCoreOAuth sample and docs/authentication.md).
// This sample makes a real network call and is not part of automated CI test execution -
// CI only verifies it compiles.
using XApiSharp;
using XApiSharp.Authentication;
using XApiSharp.Posts;

var token = Environment.GetEnvironmentVariable("X_USER_ACCESS_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set the X_USER_ACCESS_TOKEN environment variable to a user access token with tweet.write scope and try again.");
    return 1;
}

if (Environment.GetEnvironmentVariable("X_CONFIRM_WRITE") != "yes")
{
    Console.Error.WriteLine("This sample posts a real, visible Tweet to the authenticated account.");
    Console.Error.WriteLine("Set X_CONFIRM_WRITE=yes to acknowledge that and run it for real.");
    return 1;
}

var text = args.Length > 0 ? string.Join(' ', args) : $"Test post from the XApiSharp PostWrite sample ({DateTimeOffset.UtcNow:O}).";

using var httpClient = new HttpClient();
IXAuthenticationProvider auth = new BearerTokenAuthenticationProvider(token);
var client = new XApiClient(httpClient, auth);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

var response = await client.Posts.CreateAsync(new CreatePostRequest { Text = text }, cts.Token);
var created = response.Body?.Data ?? throw new InvalidOperationException("Post creation returned no data.");

Console.WriteLine($"Created post {created.Id}: {created.Text}");
Console.WriteLine("Note: writes are never automatically retried by the SDK (see docs/errors-and-retries.md) - a failure here does not necessarily mean nothing was posted.");
return 0;

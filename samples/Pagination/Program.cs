// Pagination sample (spec section 20 / section 14). Demonstrates both pagination shapes:
// the item-level convenience enumerable (flattened across pages, capped with XPaginationOptions)
// and the page-level enumerable (raw XResponse<TPage> per page, for inspecting Meta/HasErrors).
// Requires an app-only bearer token: set X_BEARER_TOKEN before running.
// This sample makes real network calls and is not part of automated CI test execution -
// CI only verifies it compiles.
using XApiSharp;
using XApiSharp.Authentication;
using XApiSharp.Pagination;
using XApiSharp.Users;

var token = Environment.GetEnvironmentVariable("X_BEARER_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine("Set the X_BEARER_TOKEN environment variable to an app-only bearer token and try again.");
    return 1;
}

using var httpClient = new HttpClient();
IXAuthenticationProvider auth = new BearerTokenAuthenticationProvider(token);
var client = new XApiClient(httpClient, auth);

// Defaults to the @XDevelopers account; pass a different user ID as the first argument.
var userId = args.Length > 0 ? args[0] : "2244994945";

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

Console.WriteLine("-- item-level: first 25 followers, flattened across pages --");
var options = new XPaginationOptions { MaxItems = 25 };
await foreach (var user in client.Users.GetFollowersAsync(new GetUsersPageRequest { UserId = userId }, options, cts.Token))
{
    Console.WriteLine($"@{user.Username}");
}

Console.WriteLine();
Console.WriteLine("-- page-level: first 2 pages, with per-page metadata --");
var pageOptions = new XPaginationOptions { MaxPages = 2 };
await foreach (var page in client.Users.GetFollowersPagesAsync(new GetUsersPageRequest { UserId = userId }, pageOptions, cts.Token))
{
    Console.WriteLine($"page: {page.Body?.Data?.Count ?? 0} users, HasErrors={page.HasErrors}, NextToken={page.Body?.Meta?.NextToken ?? "(none)"}");
}

return 0;

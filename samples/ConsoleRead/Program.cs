// Console read sample (spec section 20 / section 8.1 target interface).
// Requires an app-only bearer token: set X_BEARER_TOKEN before running.
// This sample makes a real network call and is not part of automated CI test execution -
// CI only verifies it compiles.
using XApiSharp;
using XApiSharp.Authentication;
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

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

XResponse<GetUserResponse> response = await client.Users.GetByIdAsync(
    new GetUserRequest { Id = userId },
    cancellationToken: cts.Token);

Console.WriteLine($"@{response.Body?.Data?.Username} ({response.Body?.Data?.Name})");
return 0;

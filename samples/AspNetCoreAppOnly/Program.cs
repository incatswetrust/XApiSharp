// ASP.NET Core app-only DI sample (spec section 18.1's "app-only ASP.NET Core app" DI sample).
// The whole point of this sample is how little it takes: AddXApiSharpAppOnly registers a single
// shared XApiClient (safe as a singleton - app-only auth carries no per-user token, spec 18.1),
// and endpoints just take it as a constructor/delegate parameter - no service locator, no manual
// HttpClient management.
// Requires X_CONSUMER_KEY/X_CONSUMER_SECRET (the app's OAuth 1.0a consumer key/secret, also used
// to obtain an app-only OAuth 2.0 token - see docs/getting-started.md). This sample makes real
// network calls and is not part of automated CI test execution - CI only verifies it compiles.
using XApiSharp;
using XApiSharp.Extensions.DependencyInjection;
using XApiSharp.Users;

var consumerKey = Environment.GetEnvironmentVariable("X_CONSUMER_KEY");
var consumerSecret = Environment.GetEnvironmentVariable("X_CONSUMER_SECRET");
if (string.IsNullOrWhiteSpace(consumerKey) || string.IsNullOrWhiteSpace(consumerSecret))
{
    Console.Error.WriteLine("Set X_CONSUMER_KEY and X_CONSUMER_SECRET and try again.");
    return 1;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddXApiSharpAppOnly(consumerKey, consumerSecret);

var app = builder.Build();

app.MapGet("/users/{id}", async (string id, XApiClient client, CancellationToken cancellationToken) =>
{
    var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = id }, cancellationToken: cancellationToken);
    return response.Body?.Data is { } user
        ? Results.Ok(new { user.Id, user.Username, user.Name })
        : Results.NotFound();
});

app.Run();
return 0;

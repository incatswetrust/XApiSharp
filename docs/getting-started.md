# Getting Started

## 1. Set up an X app

1. Create a project and app at the [X Developer Portal](https://developer.x.com/en/portal/dashboard).
2. Note the app's **API Key/Secret** (OAuth 1.0a consumer key/secret) and, if you'll use OAuth
   2.0, its **Client ID** (and **Client Secret** for a confidential/server-side app - leave it
   unset for a public client like a native or single-page app).
3. For an app-only read-only integration, generate a **Bearer Token** from the same dashboard -
   this is the simplest starting point and needs no user authorization step.
4. Only request the [scopes](https://docs.x.com/x-api/fundamentals/authentication/oauth-2-0/scopes)
   your app actually needs; see `docs/authentication.md` for how each auth mode maps to scopes.

## 2. Reference the package

```bash
dotnet add package XApiSharp.Net --version 1.0.0-beta.1
```

It's a prerelease version (see the repository root README Status - `1.0.0` stable follows once
live validation against the real X API is unblocked), so most tooling needs the exact
`--version`/an explicit prerelease flag rather than picking it up automatically. If you'd rather
build from source, reference the project directly instead: a `ProjectReference` to
`src/XApiSharp/XApiSharp.csproj`, or install from a local `dotnet pack` output:

```bash
dotnet pack src/XApiSharp/XApiSharp.csproj --configuration Release --output ./local-packages
dotnet add package XApiSharp.Net --source ./local-packages
```

## 3. Make your first request

The simplest working example - an app-only read, using just a bearer token (see
`samples/ConsoleRead/Program.cs` for the full, compiled version of this):

```csharp
using XApiSharp;
using XApiSharp.Authentication;
using XApiSharp.Users;

using var httpClient = new HttpClient();
IXAuthenticationProvider auth = new BearerTokenAuthenticationProvider(Environment.GetEnvironmentVariable("X_BEARER_TOKEN")!);
var client = new XApiClient(httpClient, auth);

var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "2244994945" });

Console.WriteLine($"@{response.Body?.Data?.Username} ({response.Body?.Data?.Name})");
```

Run it:

```bash
X_BEARER_TOKEN=your-bearer-token dotnet run --project samples/ConsoleRead
```

`XApiClient` owns no lifetime over the `HttpClient` you pass it (HTTP-01) - construct it the way
your app already manages `HttpClient` instances (a singleton for a console app, `IHttpClientFactory`
in ASP.NET Core - see `docs/di-and-multi-user.md`).

## 4. Where to go next

| Need | See |
| --- | --- |
| OAuth 2.0 (PKCE)/OAuth 1.0a/app-only auth, refresh, token storage | `docs/authentication.md` |
| Paged list endpoints (followers, search results, ...) | `docs/pagination.md` |
| Uploading media (images/video/GIF/subtitles) | `docs/media.md` |
| Filtered/sampled Post streams, rule management | `docs/streaming.md` |
| What gets thrown, when retries happen, rate limits | `docs/errors-and-retries.md` |
| DI registration, per-user client lifetime | `docs/di-and-multi-user.md` |
| Which of the 190 operations are implemented/tested | `docs/coverage.md` |

Every family client hangs off `XApiClient` by entity name - `client.Users`, `client.Posts`,
`client.Media`, `client.Streaming`, `client.Chat`, and so on; IntelliSense on the client instance
is the fastest way to discover what's available.

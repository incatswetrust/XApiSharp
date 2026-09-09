# Dependency injection and multi-user apps

`XApiSharp.Net.Extensions.DependencyInjection` provides `Microsoft.Extensions.DependencyInjection`
registration for two shapes of app (spec section 18.1):

- **App-only** - one shared `XApiClient`, safe to inject as a singleton, because app-only auth
  carries no per-user token.
- **Multi-user** - an `IXApiUserClientFactory` that builds a fresh `XApiClient` per application
  user on demand. There is deliberately no singleton `XApiClient` for this mode - a shared
  instance would mean one user's OAuth 2.0 token leaking into another user's requests.

Both registrations use `IHttpClientFactory` under a shared named client (`XApiSharpDefaults.HttpClientName`)
so the `HttpClient` handler is pooled, never re-created per request (spec HTTP-02).

## App-only

```csharp
builder.Services.AddXApiSharpAppOnly(consumerKey, consumerSecret);
```

Then take `XApiClient` as a constructor or minimal-API parameter - see `samples/AspNetCoreAppOnly/`
for a complete, compilable example:

```csharp
app.MapGet("/users/{id}", async (string id, XApiClient client, CancellationToken cancellationToken) =>
{
    var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = id }, cancellationToken: cancellationToken);
    return response.Body?.Data is { } user ? Results.Ok(user) : Results.NotFound();
});
```

For any other stateless authentication mode (a fixed bearer token, a custom `IXAuthenticationProvider`),
use the lower-level `AddXApiSharp(Func<IServiceProvider, IXAuthenticationProvider>, ...)` overload
that `AddXApiSharpAppOnly` itself is built on.

## Multi-user (OAuth 2.0 Authorization Code + PKCE)

```csharp
builder.Services.AddXApiSharpMultiUser(clientId, clientSecret);
```

This registers `IXApiUserClientFactory` - see `samples/AspNetCoreOAuth/` for the complete login →
callback → authenticated-request flow. The shape:

```csharp
app.MapGet("/login", (HttpContext context, IXApiUserClientFactory clientFactory) =>
{
    var authRequest = clientFactory.OAuth2Client.CreateAuthorizationRequest(redirectUri, scopes);
    // persist authRequest keyed to the browser session until the callback arrives (see docs/authentication.md)
    return Results.Redirect(authRequest.AuthorizationUrl.ToString());
});

app.MapGet("/callback", async (HttpContext context, IXApiUserClientFactory clientFactory, CancellationToken ct) =>
{
    var tokens = await clientFactory.OAuth2Client.CompleteAuthorizationAsync(authRequest, callbackUri, ct);
    var authProvider = clientFactory.GetAuthenticationProvider(userId);   // your app's own user ID
    await authProvider.SetInitialTokenAsync(tokens, ct);
    return Results.Redirect("/me");
});

app.MapGet("/me", async (string userId, IXApiUserClientFactory clientFactory, CancellationToken ct) =>
{
    var client = clientFactory.CreateForUser(userId);   // cheap - build it fresh per request
    var response = await client.Users.GetMeAsync(cancellationToken: ct);
    return Results.Ok(response.Body?.Data);
});
```

`clientFactory.CreateForUser(userId)` is cheap to call on every request - `XApiClient` is a
lightweight wrapper around a pooled `HttpClient` and that user's already-authorized
`XOAuth2UserAuthenticationProvider`. Never resolve or cache a per-user `XApiClient` yourself as a
singleton - that is exactly the "token crosses users" failure mode this design avoids.

`userId` is your own application-level identifier for the signed-in user - whatever key your app
already uses (a database ID, a claims-principal subject, etc.), not anything from X itself.

## Replacing pieces

Each of the following is registered with `TryAddSingleton`/`TryAddSingleton<TInterface, TImpl>`
internally, so registering your own implementation - before or after calling
`AddXApiSharpAppOnly`/`AddXApiSharpMultiUser` - takes over (spec 18.1: "the ability to replace the
token provider, token store, clock, and transport settings"):

- **Token store** (multi-user only) - `IXOAuth2TokenStoreFactory`. The default,
  `InMemoryXOAuth2TokenStoreFactory`, is in-process only and lost on restart - fine for local
  development, not for a multi-instance or restart-surviving deployment:

  ```csharp
  builder.Services.AddXApiSharpMultiUser(clientId, clientSecret);
  builder.Services.AddSingleton<IXOAuth2TokenStoreFactory, MyDurableTokenStoreFactory>();
  ```

- **Clock** - `TimeProvider`. Useful for tests, or if your app already centralizes on one:

  ```csharp
  builder.Services.AddSingleton<TimeProvider>(myTimeProvider);
  ```

- **Transport settings** - `XClientOptions`, via the `configureOptions` parameter on either
  registration method, or additional `services.Configure<XClientOptions>(...)` calls (cumulative,
  applied in registration order):

  ```csharp
  builder.Services.AddXApiSharpAppOnly(consumerKey, consumerSecret, options =>
  {
      options.OperationTimeout = TimeSpan.FromSeconds(45);
  });
  ```

- **HTTP handler pipeline** - add your own configuration for the same named client
  (`XApiSharpDefaults.HttpClientName`) to attach a proxy, custom TLS root, or logging handler:

  ```csharp
  builder.Services.AddHttpClient(XApiSharpDefaults.HttpClientName)
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { /* ... */ });
  ```

## What this package deliberately doesn't do

No hidden service locator inside endpoint clients (`UsersClient`, `PostsClient`, ...) - they only
ever see the `RequestExecutor` handed to them at construction, the same whether that `XApiClient`
came from DI or a plain `new XApiClient(...)` call (see `docs/getting-started.md`). DI is entirely
optional - everything in this package is a thin registration convenience over the same public
constructors available without it.

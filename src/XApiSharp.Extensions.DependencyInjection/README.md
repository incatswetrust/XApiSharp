# XApiSharp.Extensions.DependencyInjection

`Microsoft.Extensions.DependencyInjection` registration helpers for [XApiSharp.Net](https://www.nuget.org/packages/XApiSharp.Net).

**Status:** pre-release (`1.0.0-beta.*`). Registration is implemented for both
app-only (a single, shared `XApiClient`) and multi-user OAuth 2.0 Authorization Code + PKCE (an
`IXApiUserClientFactory` building a fresh, per-user `XApiClient` on demand) - see
[`docs/di-and-multi-user.md`](https://github.com/incatswetrust/XApiSharp/blob/main/docs/di-and-multi-user.md)
in the repository root for the full guide, and `samples/AspNetCoreAppOnly/`/`samples/AspNetCoreOAuth/`
for complete, compilable examples.

```csharp
// App-only
builder.Services.AddXApiSharpAppOnly(consumerKey, consumerSecret);

// Multi-user
builder.Services.AddXApiSharpMultiUser(clientId, clientSecret);
```

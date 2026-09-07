# Samples

Compilable sample applications (spec section 20), to be added as the corresponding SDK surface
lands:

- [x] Console read (`ConsoleRead/`) — E2
- [ ] ASP.NET Core OAuth 2.0 / PKCE
- [ ] User post with explicit write trigger
- [ ] Pagination
- [ ] Media upload
- [ ] Filtered stream
- [ ] Webhook receiver

`ConsoleRead` requires a real app-only bearer token (`X_BEARER_TOKEN` env var) and makes a real
network call — it is excluded from `dotnet test`/CI test execution; CI only verifies it compiles.

# Samples

Compilable sample applications (spec section 20), to be added as the corresponding SDK surface
lands:

- [x] Console read (`ConsoleRead/`) — E2
- [ ] ASP.NET Core OAuth 2.0 / PKCE
- [ ] User post with explicit write trigger
- [ ] Pagination
- [ ] Media upload
- [ ] Filtered stream
- [x] Webhook receiver (`WebhookReceiver/`) — E5

`ConsoleRead` requires a real app-only bearer token (`X_BEARER_TOKEN` env var) and makes a real
network call — it is excluded from `dotnet test`/CI test execution; CI only verifies it compiles.

`WebhookReceiver` is an ASP.NET Core minimal API demonstrating the two things X's webhook protocol
needs from your endpoint: answering the CRC challenge and verifying+rejecting on the delivered
event signature, then handing verified events off to the app quickly. It requires
`X_CONSUMER_SECRET` (the app's consumer secret) but makes no outbound network calls; like
`ConsoleRead`, CI only verifies it compiles.

# Samples

Compilable sample applications (spec section 20):

- [x] Console read (`ConsoleRead/`) — E2
- [x] ASP.NET Core OAuth 2.0 / PKCE (`AspNetCoreOAuth/`) — E7
- [x] ASP.NET Core app-only, via the DI package (`AspNetCoreAppOnly/`) — E7
- [x] User post with explicit write trigger (`PostWrite/`) — E7
- [x] Pagination (`Pagination/`) — E7
- [x] Media upload (`MediaUpload/`) — E7
- [x] Filtered stream (`FilteredStream/`) — E7
- [x] Webhook receiver (`WebhookReceiver/`) — E5

All samples make real network calls (or, for `WebhookReceiver`, expect real incoming webhook
calls) and are excluded from `dotnet test`/CI test execution — CI only verifies they compile.

- **`ConsoleRead`** — the minimal app-only read (`X_BEARER_TOKEN`). Start here.
- **`AspNetCoreOAuth`** — the full OAuth 2.0 Authorization Code + PKCE round trip
  (`/login` → X's authorize page → `/callback`) via the DI package's `AddXApiSharpMultiUser` and
  `IXApiUserClientFactory`, demonstrating the per-user isolation `docs/authentication.md` and
  `docs/di-and-multi-user.md` describe. Requires `X_CLIENT_ID` (and `X_CLIENT_SECRET` for a
  confidential client).
- **`AspNetCoreAppOnly`** — the DI package's `AddXApiSharpAppOnly`, registering a single shared
  `XApiClient` injected straight into a minimal API endpoint. Requires `X_CONSUMER_KEY`/`X_CONSUMER_SECRET`.
- **`PostWrite`** — creates a real Post. Refuses to run unless `X_CONFIRM_WRITE=yes` is set in
  addition to `X_USER_ACCESS_TOKEN` — having credentials is not treated as consent to actually
  post.
- **`Pagination`** — both pagination shapes (item-level flattened, page-level with metadata) over
  a user's followers, using `X_BEARER_TOKEN`.
- **`MediaUpload`** — chunked upload of a local file via `UploadFromStreamAsync`, with progress
  reporting. Requires `X_USER_ACCESS_TOKEN` with `media.write` scope (app-only tokens aren't
  accepted for media upload).
- **`FilteredStream`** — replaces the account's filtered-stream rules with one rule, then streams
  matching Posts until Ctrl+C. Requires `X_BEARER_TOKEN`.
- **`WebhookReceiver`** — an ASP.NET Core minimal API demonstrating the two things X's webhook
  protocol needs from your endpoint: answering the CRC challenge and verifying+rejecting on the
  delivered event signature, then handing verified events off to the app quickly. Requires
  `X_CONSUMER_SECRET`; makes no outbound network calls.

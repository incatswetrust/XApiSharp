# Changelog

All notable changes to this project are documented here. Format follows
[Keep a Changelog](https://keepachangelog.com/), and versioning follows [SemVer](https://semver.org/).

## [1.0.0-beta.2] - 2026-09-09

### Added

- Persistent, per-auth-context rate-limit state (RATE-03/04/05, issue #15): `RequestExecutor` now
  consults previously-recorded rate-limit info before sending each request and waits for the reset
  if a scope is already known to be exhausted, instead of sending a request guaranteed to fail.
  State is scoped per `IXAuthenticationProvider` instance (so one user's exhausted limit never
  affects another's) and per endpoint, with a monotonic sequence number - not response-arrival
  order - deciding which of two concurrent responses is more recent.
- `docs/final-report.md`: the full handoff report (provenance, coverage, known limitations,
  backlog).

## [1.0.0-beta.1] - 2026-09-09

Published to NuGet.org - pre-release, see `README.md` Status for the live-validation caveat
(blocked on paid X API access; `1.0.0` stable follows once that's resolved).

### Added

- `XApiClient`, grouping a typed client per entity: `Users`, `Posts`, `Lists`, `DirectMessages`,
  `Spaces`, `CommunityNotes`, `Communities`, `Articles`, `Trends`, `News`, `Usage`, `Account`,
  `General`, `Compliance`, `Connections`, `Bots`, `Broadcasts`, `Media`, `Streaming`, `Webhooks`,
  `Chat`, `Activity` - full typed coverage of all 190 in-scope operations from the official v2
  OpenAPI snapshot (100% implementation, 100% contract-tested; see `docs/coverage.md`).
- Authentication: bearer-token passthrough, app-only token acquisition, OAuth 2.0 (Authorization
  Code + PKCE, with refresh and a pluggable token store), and OAuth 1.0a signing.
- Shared transport core (`RequestExecutor`): retry/backoff with jitter, 401-refresh-then-retry,
  rate-limit-aware waiting, and a typed error hierarchy under `XApiException`.
- A shared pagination engine (`XPaginator`) and a shared streaming/NDJSON engine (`XEventStream`)
  used identically across every family that needs them, rather than per-endpoint duplication.
- Chunked media upload (`MediaClient.UploadFromStreamAsync`) with progress reporting, bounded
  segment buffering, and processing-status polling.
- Webhook CRC-challenge and signature-verification helpers, plus an ASP.NET Core sample receiver.
- `XApiSharp.Net.Extensions.DependencyInjection`: `AddXApiSharpAppOnly`/`AddXApiSharpMultiUser`
  registration extensions, an `IHttpClientFactory`-backed named client, an `IXApiUserClientFactory`
  for building a fresh per-user client under OAuth 2.0 without ever capturing a user's token in a
  singleton, and a replaceable `IXOAuth2TokenStoreFactory`/`TimeProvider`/`XClientOptions`.
- Diagnostics (`System.Diagnostics.ActivitySource`/`Meter`, both named `"XApiSharp"`): a span per
  logical request plus events for retries, rate-limit waits, token refreshes, stream
  connect/disconnect, and deserialization failures; request/duration/retry/error counters, active
  stream-connection and dropped-duplicate-event counts. Tags are limited to the HTTP method and a
  best-effort route template - never a user ID, full URL, token, or Post/message text.
- Full sample set: console read, ASP.NET Core OAuth 2.0/PKCE and app-only (via the DI package),
  pagination, media upload, filtered stream, a Post-writing sample gated behind an explicit
  confirmation env var, and the webhook receiver.
- Full test suite: unit, contract, and opt-in soak/integration test projects (soak tests verify
  bounded memory under a 500,000-event stream and a 200 MiB chunked upload; integration tests are
  scaffolded with a fixture/cleanup policy, awaiting live API access to actually run).

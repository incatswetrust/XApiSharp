# ADR 0003: Public API and Error Model

**Status:** decided (stage E0), pending validation once real endpoints/tests land in E2–E3.

## Problem

Before generating or hand-writing any endpoint method, the shape of the public surface — client
entry point, request/response envelope, exception hierarchy — has to be fixed, because 190
operations will be built against it. Re-shaping this after bulk implementation starts would be a
breaking change touching most of the codebase.

## Decision

Core public types (spec section 8.1):

| Type | Role |
| --- | --- |
| `XApiClient` | Entry point; groups endpoint clients by entity (`client.Users`, `client.Posts`, ...) |
| `XClientOptions` | Timeouts, retry, buffer limits, allowed service base URLs |
| `IXAuthenticationProvider` | Prepares auth for a single request |
| `XOAuth2Client` | Authorization URL, code exchange, refresh, revoke |
| `XOAuth1Client` | OAuth 1.0a flows and signing |
| `XResponse<TBody>` | Typed body + HTTP metadata (status, diagnostic headers, rate-limit info, partial-success flag) |
| `XRateLimitInfo` | Nullable rate-limit fields as available from headers |
| `XRequestOptions` | Per-call overrides without mutating the shared client |
| `XApiException` (+ derived types) | Service/transport/protocol error hierarchy |

API shape rules (API-01..10): every network call is `...Async` + `CancellationToken`; parameter
objects instead of long positional-optional-argument lists; operations grouped by user-facing
entity, never duplicated across ambiguous groups; request/options objects are immutable once
passed in; the client is safe under concurrent use with per-call/per-context isolation (no
cross-user/cross-app state bleed); the golden path never requires manual `object` casts, hand-built
JSON, or hand-built URLs; a low-level generic `SendAsync` escape hatch is allowed *alongside*, never
*instead of*, typed coverage; public members document purpose/scopes/access/cancellation/errors;
the SDK never opens a browser or starts an HTTP server implicitly; missing required parameters are
caught before the request is sent, without turning volatile server-side rules into overly strict
client-side validation.

Error hierarchy (spec section 12.2) — one flat, closed-for-typical-use hierarchy under
`XApiException`:

| Situation | Type |
| --- | --- |
| API returned a non-success HTTP status | `XApiException` (carries the original error structure) |
| Invalid/expired auth | `XAuthenticationException` |
| Insufficient scopes/access | `XAccessDeniedException` |
| Temporary rate limit | `XRateLimitException` (wait hint if known) |
| Transport failure, no HTTP response | `XTransportException` (original cause preserved) |
| Response doesn't match the expected contract | `XProtocolException` |
| Internal operation deadline exceeded | `XRequestTimeoutException` |
| User-requested cancellation | standard `OperationCanceledException`, correctly linked to the token |

Derived API exceptions preserve status, problem type, details, and request ID. Billing/quota
errors are classified from the documented body/code, never assumed from the bare HTTP status (not
every 403 is billing, not every 429 is a short-lived rate limit).

Models/serialization ground rules (SER-01..12, spec section 9) apply uniformly across every
generated and hand-written model: opaque string IDs, `DateTimeOffset` with contract-specified
precision, an explicit optional-value model to distinguish missing/null/default, exact JSON field
names preserved regardless of C# naming, `fields`/`expansions`/`includes`/`meta` supported,
open/extensible enums preserve unknown values instead of throwing, `oneOf`/`anyOf`/`allOf` handled
to the extent the snapshot actually uses them, unknown fields preserved via extension data,
`JsonElement` reserved for genuinely open/unknown parts (not as a blanket escape from typing).

## Rejected alternatives

- **A single generic `XResponse` forcing every body into a `data`/`meta`/`errors` shape.**
  Rejected: several operations return empty (204), binary, or otherwise non-standard bodies (spec
  section 9); forcing one shape would require lying about the contract for those operations.
- **Throwing a single generic `HttpRequestException`-style exception for everything.** Rejected:
  callers need to distinguish "retry me," "re-authenticate," "you don't have access," and "this is
  permanently broken" programmatically (spec section 13.2's retry rules depend on exactly this
  distinction) — a flat exception type would push that classification logic into every consumer.
  A closed hierarchy under one root (`XApiException`) was chosen over a large flat set of unrelated
  exception types so callers can still catch broadly when they don't care about the distinction.
- **Positional-parameter methods for endpoints with several optional inputs.** Rejected per
  API-02 — X API v2 operations routinely have many optional query parameters
  (`fields`/`expansions`/pagination/etc.); positional overload sets would be unreadable and
  unstable across regenerations.
- **Modeling unknown/open enum values as a throw-on-deserialize failure.** Rejected per SER-06: X
  adding a new enum value server-side must not be a breaking failure for already-installed SDK
  versions; the closed-enum failure mode was explicitly called out in the spec as unacceptable.

## Consequences

- Every endpoint method generated or hand-written from E2 onward must return `XResponse<TBody>`
  (or a streaming/pagination wrapper around it) and throw only from the fixed hierarchy above —
  this ADR is the contract that `tools/XApiSharp.CodeGen` templates and manual core code both have
  to honor.
- Because the enum/extension-data rules (SER-06/SER-08) are decided now, the code generator PoC in
  E1 (ADR 0002) needs to be evaluated specifically against its ability to produce this shape, not
  just against "does it compile."
- This ADR does not yet fix concrete C# signatures for individual endpoints (e.g. exact request
  type names) — those are decided per-operation during E2–E5 and recorded in
  `spec/endpoint-manifest.json`'s `methodName`/`requestType`/`responseType` fields, not here.

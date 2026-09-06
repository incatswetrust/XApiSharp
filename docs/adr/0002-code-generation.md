# ADR 0002: Code Generation Approach

**Status:** decided (stage E1 PoC complete, 2026-09-06).

## Problem

Need to pick how the ~190-operation, ~660-schema surface gets turned into C# without hand-writing
every DTO, while still meeting GEN-01..GEN-10 (spec section 7.2) — in particular GEN-05: an
unsupported schema construct must fail generation loudly, never silently degrade to something that
compiles but is wrong or empty.

## PoC method

Installed NSwag 14.7.1 as a version-pinned local tool (`.config/dotnet-tools.json`, `nswag.consolecore`)
and ran `openapi2csclient` directly against `spec/openapi-snapshot.json` (190 operations, 554
schemas), twice: once with default settings (Newtonsoft.Json) and once with
`/JsonLibrary:SystemTextJson`. Then inspected the output specifically for the constructs called out
in spec section 7.1: `$ref` resolution, nullable, unions (`oneOf`/`anyOf`/discriminators), multipart,
varied response shapes, and general shape of the generated client relative to the transport core
this project needs to own per ADR 0001/0003.

## Findings

**Positive:**

- `/JsonLibrary:SystemTextJson` works as advertised — spot-checked classes use
  `System.Text.Json.Serialization.JsonPropertyName`/`JsonExtensionData`; the only remaining
  "Newtonsoft" strings in that output are toolchain-attribution comments
  (`[GeneratedCode("NJsonSchema", "...(Newtonsoft.Json v13.0.0.0)")]`), not a runtime dependency.
  No conflict with the `System.Text.Json`-only decision in ADR 0001.
- Plain CRUD-shaped DTOs and operations (the large majority — e.g. `ActivityStreamResponse`,
  `CreateUsersBookmarkResponse`'s `data` wrapper, etc.) generate cleanly: correct property names via
  `JsonPropertyName`, `JsonExtensionData` for unknown fields (satisfies SER-08), reasonable
  nullability.
- No bare `System.Object` fallbacks were found anywhere in either generated file (`grep -c
  "System.Object\b"` → 0) — NSwag doesn't take the "replace with object" shortcut GEN-05 explicitly
  forbids.
- Ran with **zero warnings or errors** on both runs — confirms NSwag does not report the problems
  below on its own; any GEN-05 enforcement has to be built by this project, not assumed from the
  tool.

**Confirmed problems (three concrete cases, all reproduced against the real snapshot):**

1. **`oneOf`/discriminated unions silently become empty classes.** 10 top-level schemas in the
   snapshot use `oneOf` (2 of them — `ActivityStreamingResponsePayload`, `Problem` — with a
   `discriminator`, up to 9 branches). All 10 were generated as `public partial class X { }` — zero
   properties, zero polymorphic deserialization, zero indication anything was lost. Critically,
   **`Problem` is the schema this project's `XApiException` hierarchy (ADR 0003) needs to read
   status/type/detail/request-id from** — NSwag cannot produce that type usably at all.
2. **Multipart operations with a JSON/multipart content-type alternative silently drop the file
   parameter and the multipart branch entirely.** All 3 such operations in the snapshot
   (`POST /2/media/upload`, `POST /2/media/upload/{id}/append`,
   `POST /2/chat/media/upload/{id}/append` — all declare `["application/json",
   "multipart/form-data"]`) generate a method that accepts the file parameter (e.g. `Media3 media`)
   but never references it when building the request — the generated body always serializes as
   `application/json` and the multipart path plus the actual binary payload are gone, with no error.
   The generated method compiles and looks complete, which is worse than an obvious gap.
3. Both problems are **silent** — no NSwag warning/error/log line flags either case on either run.

**Scope of the damage is small and already isolated by other decisions:** exactly 10 schemas and 3
operations are affected out of 190 operations / 554 schemas. All 3 multipart operations are Media
endpoints that spec section 15 (MEDIA-01..12: `Stream`-based transfer, chunked upload sequencing,
progress callback, non-seekable handling) already mandates hand-writing regardless of generator
choice — so finding #2 does not add net-new hand-written scope, it just confirms the plan. The 10
`oneOf` schemas are error/compliance-event payloads that belong in the hand-written core's error
model (ADR 0003) or a small set of manually-modeled compliance/stream-event types, not in bulk
generated code, by design.

## Decision

**Adopt NSwag 14.7.1 (`NSwag.ConsoleCore`, pinned via local tool manifest) with
`/JsonLibrary:SystemTextJson`, wrapped by `tools/XApiSharp.CodeGen`, for the majority of DTOs and
endpoint-client scaffolding.** Do not evaluate an XDK-extension or custom-generator alternative
(spec section 7.1's fallback path) — NSwag's failures are narrow, enumerable, and already covered
by hand-written-core scope from other ADRs; switching tools would not remove the need to hand-write
`Problem`/media upload anyway.

`tools/XApiSharp.CodeGen` (not NSwag itself) is responsible for GEN-05 compliance, since NSwag
won't provide it:

- **Union guard:** before/after invoking NSwag, scan `spec/openapi-snapshot.json` for every
  top-level schema with `oneOf`/`anyOf`/`discriminator`. Each one must have a corresponding entry in
  `spec/overrides/` documenting it as hand-modeled (with the manual type name), or the generation
  step **fails the build** naming the unresolved schema. Never silently accept an empty generated
  class for one of these.
- **Multipart guard:** scan for operations whose `requestBody.content` has more than one media type
  where at least one is `multipart/form-data`. Each must have a registered manual override (endpoint
  implemented by hand in the core, not via the generated client method) or generation **fails the
  build**.
- Both guards produce the GEN-07 report (operations/models/exceptions/unmapped elements) and are
  covered by CI's GEN-08 no-undocumented-method check.
- The 10 known `oneOf` schemas and 3 known multipart operations are the **initial** override list
  (`spec/overrides/e1-generation-guard.md`, to be added when the wrapper is implemented) — this is
  not assumed to be exhaustive forever; the guard re-scans on every snapshot refresh (section 3.4)
  so a newly introduced `oneOf` schema or multipart operation is caught, not missed.
- Generated files keep the autogeneration marker (GEN-04); NSwag's own
  `[GeneratedCode(...)]` attribute already provides this at the class level, but
  `tools/XApiSharp.CodeGen` also stamps a file-header comment per GEN-04's repo-level convention.

## Rejected alternatives

- **Default NSwag settings (Newtonsoft.Json).** Rejected: conflicts with the `System.Text.Json`
  decision in ADR 0001; the `SystemTextJson` setting was verified to work with no loss versus
  default, so there's no reason to accept the Newtonsoft dependency.
- **Switching to an XDK-extension generator or a custom generator on OpenAPI.NET** (spec 7.1's
  documented fallback). Rejected for now: the problems found are narrow (10 schemas, 3 operations)
  and already-planned hand-written scope covers them; a from-scratch or XDK-extension generator
  would cost far more engineering time than writing 10 override types and a generation guard, for a
  benefit that's still unproven (no evidence a different generator handles X's specific
  `discriminator` shape — note the discriminator `propertyName` is the unusual relative path
  `../event_type`, not a plain field name — any generator would need special-case handling here,
  not just "a better generator").
- **Silently accepting the empty-class/dropped-multipart output and patching it later as bugs are
  found.** Rejected: this is exactly the failure mode GEN-05 exists to prevent — an empty `Problem`
  class would compile fine and only fail at runtime when the first real error response arrives,
  which is a worse failure mode than a build-time error naming the exact schema.

## Consequences

- `tools/XApiSharp.CodeGen` is not a thin NSwag wrapper — it owns real validation logic (the two
  guards above) that this ADR makes load-bearing for GEN-05 compliance. This must be implemented
  before generation is trusted in CI, not treated as a nice-to-have.
- The hand-written core (already scoped by ADR 0003 for the error hierarchy, and by spec section 15
  for media) now has a concrete, enumerated list of exactly which generated-schema gaps it must
  fill: `Problem` and the other 9 `oneOf` schemas, plus the 3 multipart media operations.
- Every future snapshot refresh must re-run both guards before regeneration is accepted — a newly
  introduced union or multipart+alternate-content-type operation is a build failure, not a silent
  gap, until someone adds the corresponding override.

# Product Scope

Status: **initial inventory complete (stage E0)**, pending manual per-operation review.

## Snapshot

- Source: `https://api.x.com/2/openapi.json`
- Retrieved: 2026-09-06T18:22:51Z
- SHA-256: `68f24f5f2332bd60127a86e46f5f34d4b45fc1ad4ff19a91b1d24c3ccda7f995`
- OpenAPI version: `3.0.0`; upstream `info.version`: `2.168`
- See `spec/spec-manifest.json` for full provenance fields.

## Inventory result

- **156 paths / 190 operations** discovered.
- **0 deprecated** operations found in this snapshot.
- Auth/scopes are read directly from each operation's `security` requirement in the snapshot
  (not inferred) — 189/190 operations declare explicit security; `GET /2/openapi.json` is public
  (no security requirement, it serves the spec document itself).
- Security schemes present: `BearerToken` (app-only bearer), `OAuth2UserToken` (Authorization
  Code + PKCE, full scope list embedded in the snapshot), `UserToken` (OAuth 1.0a, scheme name
  `OAuth`).

## Operations by family (OpenAPI tag, `/bookmarks` paths reclassified per section 3.1)

| Family | Count |
| --- | --- |
| Users | 36 |
| Stream | 18 |
| Chat | 16 |
| Posts | 14 |
| Broadcasts | 13 |
| Media | 11 |
| Direct Messages | 9 |
| Lists | 9 |
| Webhooks | 8 |
| Bookmarks | 6 |
| Bots | 6 |
| Compliance | 6 |
| Spaces | 6 |
| Account Activity | 5 |
| Activity | 5 |
| Community Notes | 5 |
| Connections | 4 |
| Account | 2 |
| Articles | 2 |
| Communities | 2 |
| News | 2 |
| Trends | 2 |
| Usage | 2 |
| General | 1 |
| **Total** | **190** |

`X Ads` is absent from this snapshot, consistent with Ads being a separate product excluded per
section 3.3.

`Marketplace` is **not a tag in the OpenAPI snapshot**, but it does exist on the docs.x.com side:
one page, "Get Marketplace Handle Availability", with no rendered description or contract detail.
See `spec/overrides/e0-docs-cross-check.md` — no contract to implement against; tracked as an open
item, not silently dropped.

Two families are new relative to the section 3.1 audit table. A docs.x.com cross-check (see
`spec/overrides/e0-docs-cross-check.md`) found **zero public documentation for either family** —
not a documentation gap on a couple of endpoints, the entire family is undocumented on docs.x.com:

- **Broadcasts** (13/13 ops undocumented) — X Live broadcast management (scheduling, going live,
  broadcast chat). Present in the official v2 snapshot with full auth/scope requirements, no match
  to any 3.3 exclusion — stays `inScope: true` per section 3.1 (snapshot presence is sufficient).
  But flag to the project owner before investing significant E4/E5 effort here: an undocumented
  surface already in the snapshot is more likely to be unreleased/preview and to change shape
  without notice, and this project has no way to live-validate it without documented behavior to
  check against.
- **Bots** (6/6 ops undocumented) — bot account create/update/delete/token management. Same status
  and same caveat.

`Account` (developer account, 2 ops) is also fully undocumented on docs.x.com — same caveat, lower
concern (plausibly a developer-dashboard-only surface). `General` (the `/2/openapi.json` meta
endpoint) is trivially in scope. A handful of single operations in otherwise well-documented
families (3 Compliance job upload/download/cancel ops, 1 Usage credits op, 1 Chat delete-messages
op, 1 Bookmarks create-folder op) are also undocumented — most likely plain doc coverage gaps, not
a scope signal; see the override file for the full list.

## Scope decisions applied so far

All 190 discovered operations are marked `inScope: true` in `spec/endpoint-manifest.json` with
the mechanical reason "found in the official v2 snapshot, no match to a section 3.3 exclusion."
**This is a first pass, not a final per-operation review.** None of the hard exclusions from
section 3.3 (Ads API, legacy v1.1/GNIP, internal GraphQL/scraping, X Chat E2E crypto
implementation) were encountered as distinct operations in this snapshot, which is expected since
the source is specifically the public v2 HTTP surface.

Done (see `spec/overrides/e0-docs-cross-check.md` for full detail):

1. ✅ Cross-checked the discovered operation list against `https://docs.x.com/x-api/llms.txt`
   (258 candidate reference pages fetched 2026-09-06). Zero operations documented-with-a-contract
   are missing from the OpenAPI snapshot. Three docs.x.com pages describe a feature with no
   published contract (AI trend by ID, Account Activity replay job, Marketplace handle
   availability) — tracked as open items, not implemented against a guess.
2. ✅ `docsReference` filled in `spec/endpoint-manifest.json` for 163/190 operations (the ones with
   a matched doc page); 27 operations have no discoverable public doc page — see the Broadcasts/
   Bots/Account caveat above.

Still outstanding:

1. Decide with the project owner whether to proceed with Broadcasts/Bots at full priority in E4/E5
   given the lack of public documentation, or deprioritize them relative to documented families.
2. Review the heuristic `flags` (pagination/streaming/upload/multipart) per operation during
   E4/E5 implementation — they are not authoritative yet.

## Naming decision (package/repo)

Working package ID `XApiSharp` is **already registered on NuGet.org by an unrelated package**
(unaffiliated Chinese-language "XAPI" wrapper, author `hetao`). Per user decision, the project
package ID is **`XApiSharp.Net`** (main package) and **`XApiSharp.Net.Extensions.DependencyInjection`**
(DI package); both confirmed available on NuGet.org as of this inventory. The C#
namespace/repository name remains `XApiSharp` — only the NuGet `PackageId` differs.

## Still open from E0 (not started)

- Competitor/prior-art review (`docs/competitor-review.md`) — official XDK/.NET solutions status.
- ADR 0001–0003 — runtime/package layout, code generation approach, public API/error model.
- Confirmed actual X test application, auth setup, and real-request budget for later live
  validation (section 19.4).

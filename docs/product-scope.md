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

Families not present as a distinct tag in this snapshot: **Marketplace** and **X Ads** — neither
appears in the fetched document, consistent with Ads being a separate product (excluded per
section 3.3) and Marketplace not currently surfaced as a distinct v2 HTTP group here. If a
`Marketplace`-labeled surface is found under a different name during manual review, register it
explicitly rather than silently omitting it.

Two families are new relative to the section 3.1 audit table and are not yet classified against
section 3.3 exclusions by a human:

- **Broadcasts** (13 ops) — X Live broadcast management (scheduling, going live, broadcast chat).
  Publicly present in the official v2 snapshot; no obvious match to any 3.3 exclusion. Tentatively
  in scope, pending confirmation against current public docs.
- **Bots** (6 ops) — bot account create/update/delete/token management. Same status: tentatively
  in scope, pending doc confirmation.

`Account` (developer account) and `General` (the `/2/openapi.json` meta endpoint) are also new
relative to the table; both are trivially in scope as ordinary documented v2 HTTP operations.

## Scope decisions applied so far

All 190 discovered operations are marked `inScope: true` in `spec/endpoint-manifest.json` with
the mechanical reason "found in the official v2 snapshot, no match to a section 3.3 exclusion."
**This is a first pass, not a final per-operation review.** None of the hard exclusions from
section 3.3 (Ads API, legacy v1.1/GNIP, internal GraphQL/scraping, X Chat E2E crypto
implementation) were encountered as distinct operations in this snapshot, which is expected since
the source is specifically the public v2 HTTP surface.

Still outstanding before this counts as a reviewed registry (section 4, E0 items 3 and 5):

1. Cross-check the discovered operation list against `https://docs.x.com/x-api/llms.txt` to catch
   anything documented but missing from the OpenAPI snapshot (or vice versa), and record any
   snapshot/docs disagreement as an override with its source.
2. Manually confirm the tentative inclusion of Broadcasts and Bots against current public
   documentation (not just the OpenAPI tag).
3. Fill in `docsReference` for each operation (link to the specific reference page).
4. Review the heuristic `flags` (pagination/streaming/upload/multipart) per operation during
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

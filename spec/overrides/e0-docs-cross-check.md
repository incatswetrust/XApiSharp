# E0 override: OpenAPI snapshot vs. docs.x.com cross-check

**Date:** 2026-09-06
**Sources compared:**
- `spec/openapi-snapshot.json` (SHA-256 `68f24f5f2332bd60127a86e46f5f34d4b45fc1ad4ff19a91b1d24c3ccda7f995`)
- `https://docs.x.com/x-api/llms.txt` (fetched 2026-09-06)

**Method:** Parsed the docs index into 29 categories, discarded categories that are structurally
out of scope (`Enterprise Gnip 2.0`, `Fundamentals`, `Getting Started`, `Main`, `Migrate`, `Agent
Resources`) and pages that are guides/quickstarts/migration write-ups rather than operation
reference pages, leaving 258 candidate reference pages. Fetched each as Markdown and extracted the
embedded `` ```yaml <method> <path> `` OpenAPI block where present. Matched the resulting
method+path against `spec/endpoint-manifest.json` by exact key.

Full raw fetch results are not committed (too large / regenerable); this file records the
conclusions. Re-running this check is mechanical — repeat the fetch-and-match against a fresh
`llms.txt` and diff against this list.

## Result 1: zero operations documented-with-a-contract are missing from the OpenAPI snapshot

258 candidate pages → 170 had an embedded contract → all 170 matched an existing
`spec/endpoint-manifest.json` entry (163 unique operations; a handful of operations have more than
one doc page, e.g. "Create Post" and "Create Or Edit Post" both document `POST /2/tweets`). **No
snapshot/docs disagreement of this kind found.** The OpenAPI snapshot is not missing anything that
docs.x.com documents with a concrete method+path contract.

## Result 2: three doc pages describe a feature with no published contract at all

These pages have a title and a one-line description but **no OpenAPI block, no method, no path** —
i.e. docs.x.com prose promises a feature that isn't in the OpenAPI snapshot. Do not implement these
by guessing a path/shape. Track as open items; re-check on the next snapshot refresh.

| Feature | Doc page | Note |
| --- | --- | --- |
| Get AI trend by ID | `https://docs.x.com/x-api/trends/get-ai-trends-by-id.md` | "Retrieve a single AI-curated trend by its identifier..." — no contract published. |
| Create Account Activity replay job | `https://docs.x.com/x-api/account-activity/create-replay-job.md` | Explicitly on a **deprecated** API (Account Activity API, superseded by the Activity API / XAA) — likely intentionally left undocumented as the product winds down, not an oversight. |
| Get Marketplace Handle Availability | `https://docs.x.com/x-api/marketplace/get-marketplace-handle-availability.md` | No description or contract rendered at all. `Marketplace` has no corresponding tag/operation anywhere in the OpenAPI snapshot. |

**Decision:** not in scope for now — no contract exists to implement against. Re-check at each
snapshot refresh (spec section 3.4); if a contract appears, register it as a new operation for the
next minor release rather than backfilling 1.0 scope.

## Result 3: 27 snapshot operations have no discoverable public doc page at all

These exist in the OpenAPI snapshot (so they satisfy inclusion under section 3.1 — "found in the
official snapshot" is sufficient) but do not appear anywhere in `docs.x.com/x-api/llms.txt`'s
reference index:

| Family | Count | Operations |
| --- | --- | --- |
| Broadcasts | 13 | all of them — `/2/broadcasts*` (list/scheduled/chat management) |
| Bots | 6 | all of them — `/2/bots*` (create/update/delete/token) |
| Account | 2 | `GET /2/account`, `POST /2/account` (developer account) |
| Compliance | 3 | `DELETE .../jobs/{id}`, `GET .../jobs/{id}/download`, `PUT .../jobs/{id}/upload` |
| Usage | 1 | `GET /2/usage/credits` |
| Chat | 1 | `POST /2/chat/conversations/{id}/messages/delete` |
| Bookmarks | 1 | `POST /2/users/{id}/bookmarks/folders` |

**Interpretation:**

- The single-operation gaps (Compliance, Usage, Chat, Bookmarks) are most likely plain documentation
  coverage gaps in an otherwise fully-documented family (their sibling operations in the same family
  *are* documented) — low risk, proceed as in-scope.
- **Broadcasts (13/13) and Bots (6/6) have *zero* public documentation for the entire family.**
  Combined with Account (2/2), this is a stronger signal than a documentation gap: these may be
  unreleased, preview, internal-dashboard-only, or otherwise not-yet-public surfaces that happen to
  already be present in the OpenAPI snapshot. Per section 3.1 the snapshot alone is sufficient
  grounds for inclusion, and per section 3.3 "no public docs" is not a listed exclusion reason — so
  they remain `inScope: true` in the registry. But this should be flagged to the project owner
  before committing significant E4/E5 implementation effort to Broadcasts/Bots specifically, since
  undocumented surfaces are more likely to change shape without notice or require access this
  project won't have for live validation.

No registry entries were changed to `inScope: false` as a result of this check — this is a
documentation cross-check, not a new scope decision. See `docs/product-scope.md` for the resulting
open items.

# XApiSharp.IntegrationTests

Real requests against the live X API (spec section 19.4). **Disabled by default.**

Running these tests requires a dedicated test X application, explicit environment configuration
(credentials, allowed scopes, request budget), and is never executed automatically in normal CI.
**No such configuration exists yet, and no paid API access is currently available for this
project** - `spec/endpoint-manifest.json` records every operation's `liveValidation.status` as
`blocked-by-budget` with that reason (see `docs/coverage.md`). This project currently contains no
live-scenario tests; it has `CreatedObjectLog` (below), the fixture/cleanup infrastructure spec
19.4 requires, verified deterministically without live access, ready for whoever picks this up
once credentials exist.

## Fixture and cleanup policy (spec 19.4)

> Fixture data must be synthetic or explicitly permitted-to-store and de-identified. Never commit
> DMs, tokens, or private user responses. Cleanup deletes only objects created by the current test
> run; the IDs of such objects are recorded in the run log.

Concretely, when writing a live test:

- Create fixtures the test itself owns (a Post/List/etc. this run creates), not real user content
  you found by browsing the API - never capture and commit a real DM, token, or another user's
  private response as a fixture or into a recorded test artifact.
- Use `CreatedObjectLog` (`CreatedObjectLog.cs`) to track everything the test creates: call
  `log.Track(family, id, deleteCallback)` immediately after each create call succeeds, before any
  further assertion that might throw - a test that fails partway through must still know what it
  made. `await using` the log so cleanup runs even on failure; it deletes tracked objects
  newest-first (in case a later object depends on an earlier one) and logs every create/delete/
  delete-failure to `ITestOutputHelper`, which lands in the CI/local test run's own log - satisfying
  "IDs of created objects are recorded in the run log" without a separate mechanism.
- Cleanup is deliberately scoped to exactly what `CreatedObjectLog` tracked for *this* run - never
  broaden it to "delete everything matching some pattern" on the test account, which risks removing
  another concurrent run's or a human's fixtures.
- A failed delete is logged, not swallowed, and does not stop the rest of the run's objects from
  being cleaned up (`CreatedObjectLogTests` covers this deterministically) - a real leak in the
  test X app should stay visible instead of being hidden by a broad try/catch.

## Required minimum before declaring 1.0 stable (spec 19.4)

Not yet run - blocked on paid API access. Checklist for whoever runs these once it's available:

- [ ] One app-only read scenario, where that auth mode is available for the operation.
- [ ] A real user OAuth 2.0 flow plus a successful user-authorized request; refresh checked against
      an actually-issued session, not a mocked one.
- [ ] One permitted write-then-delete cycle on a self-owned test object (via `CreatedObjectLog`).
- [ ] A real OAuth 1.0a scenario, if that mode is claimed as supported.
- [ ] One end-to-end scenario per available media/streaming/webhook protocol actually in use.
- [ ] At least one representative live scenario per available family; unavailable operations stay
      in `spec/endpoint-manifest.json` with an honest status and reason - contract-tested is not
      the same as "verified in production," and the two must not be conflated.

Per spec 19.4: a release cannot be called "passed the required minimum" without the read,
user-authorization, and write scenarios above at least. Where access is genuinely unavailable,
prepare everything else and name the specific stable-release blocker rather than skipping silently.

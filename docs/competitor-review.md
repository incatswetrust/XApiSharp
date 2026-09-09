# Competitor / Prior-Art Review

**Date:** 2026-09-06. All links and stats below were checked live on this date via the GitHub and
NuGet.org APIs; re-verify before relying on this for a go/no-go decision much later.

## Official X SDK generator (XDK)

- Repo: https://github.com/xdevplatform/xdk
- MIT-licensed Rust-based OpenAPI SDK generator, owned by X itself.
- Ships exactly two generated SDKs today: **Python** (`pip install xdk`) and **TypeScript**
  (`npm install @xdevplatform/xdk`) — confirmed from the repo README's SDK table
  (https://github.com/xdevplatform/xdk#sdks) and by grepping the README for
  `csharp|c#|.net|dotnet` (zero matches).
- **No official C#/.NET SDK exists or is planned in this repo as of this date.** This is the
  primary justification for XApiSharp.Net existing at all: there is no first-party alternative for
  .NET.
- The generator vendors the raw OpenAPI snapshot itself (`specs/openapi.json`) alongside the
  MIT-licensed generator code — see `spec/README.md` for how that precedent applies to this
  repository's `spec/openapi-snapshot.json`.
- Separately, X also publishes the `chat-xdk` repo for X Chat end-to-end encryption (including
  .NET bindings for the crypto layer) — https://github.com/xdevplatform/chat-xdk. Out of scope for
  this project's 1.0 (see the spec section 3.2); a possible future integration point, not something
  to duplicate.

## Existing community .NET libraries

| Package | NuGet downloads | GitHub stars | Last push | License | Notes |
| --- | --- | --- | --- | --- | --- |
| [LinqToTwitter](https://www.nuget.org/packages/linqtotwitter) ([repo](https://github.com/JoeMayo/LinqToTwitter)) | 4.39M | 510 | 2026-04-15 (active) | MIT | LINQ-provider design (`TwitterContext` ≈ EF `DbContext`, `IQueryable<T>` queries). Async. Actively maintained — most credible existing alternative. |
| [TweetinviAPI](https://www.nuget.org/packages/TweetinviAPI) ([repo](https://github.com/linvi/tweetinvi)) | 6.08M | 999 | 2024-08-11 (**stale, ~2 years**) | MIT | Highest download count, but no push in ~2 years as of this review — maintenance risk, consistent with community reports of it struggling to keep up after X's 2023 pricing/access changes. |
| [TwitterSharp](https://www.nuget.org/packages/TwitterSharp) ([repo](https://github.com/Xwilarg/TwitterSharp)) | 47.8K | 70 | 2023-09-27 (**stale, ~3 years**) | MIT | Explicitly "C# wrapper around Twitter API V2," net5.0 target. Small, unmaintained. |
| [TweetSharp](https://www.nuget.org/packages/TweetSharp) / TweetSharp-Unofficial | 942K / 478K | — | — | — | Targets the legacy (pre-v2) Twitter API. Not a v2 competitor; listed only because of its download count. |

## What XApiSharp.Net does differently

Based on the above (not a claim of superiority on unverified points — only what's checked):

1. **No existing library is generated from + tracked against the official OpenAPI snapshot with a
   published coverage matrix.** LinqToTwitter and Tweetinvi both predate the current v2 OpenAPI
   document as a source of truth and appear to be hand-maintained; this project's registry
   (`spec/endpoint-manifest.json`) is built directly from the snapshot and is meant to stay
   verifiably in sync with it (spec section 5).
2. **No .NET library found claims explicit coverage of the newer v2-only surfaces** this project's
   inventory already found in the snapshot: Chat HTTP transport (16 ops), Communities, Community
   Notes, Articles, Trends (incl. AI trends), Broadcasts, Bots. (Not verified as *absent* from
   LinqToTwitter/Tweetinvi — just not found in the top-level materials checked; a deeper pass
   before implementing those families could still find partial coverage worth crediting or
   learning from.)
3. **Maintenance risk in the two most-downloaded alternatives**: Tweetinvi is stale; LinqToTwitter
   is active but is a single-maintainer project with a different API paradigm (LINQ provider) than
   the typed-client-with-DI-support target shape in this spec (section 8).
4. **No official first-party .NET SDK exists**, unlike Python/TypeScript — this project fills that
   specific gap rather than competing with an official offering.

## Explicitly not re-verified in this pass (would need deeper research before quoting externally)

- Whether LinqToTwitter or Tweetinvi have partial support for chunked media upload, filtered
  streams, or webhooks, and how complete/tested that support is.
- Actual current .NET TFM support matrix for either project (README claims were not exhaustively
  checked against their `.csproj` files).
- Whether either project has since diverged in scope (e.g., dropped/added families) after this
  review's date.

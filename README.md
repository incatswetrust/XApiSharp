# XApiSharp

An independent, community-maintained C#/.NET SDK for the public [X API v2](https://docs.x.com/x-api).

> **This project is not affiliated with, endorsed by, or sponsored by X Corp.** "XApiSharp" is a
> working name; the final package/repository name depends on availability checks (stage E0).

## Status

🚧 **Early scaffolding — no endpoints are implemented yet.** This repository currently contains
the solution layout, build configuration, and CI skeleton only. It is not installable from
NuGet.org, and the interface shown anywhere in the docs is a target, not something you can use
today.

Work proceeds in stages E0–E10 (inventory → vertical slice → full endpoint coverage → hardening →
release → maintenance). See `docs/` for the design decisions (ADRs) and the operation coverage
matrix once they exist.

## Repository layout

| Path | Purpose |
| --- | --- |
| `src/XApiSharp/` | Main package: models, endpoint clients, transport, core mechanisms |
| `src/XApiSharp.Extensions.DependencyInjection/` | `Microsoft.Extensions.DependencyInjection` integration |
| `tools/XApiSharp.CodeGen/` | Dev-time code generation / coverage report CLI (not shipped to consumers) |
| `tests/XApiSharp.UnitTests/` | Core logic (auth, retry, parsing, pagination) |
| `tests/XApiSharp.ContractTests/` | Per-operation contract and serialization checks |
| `tests/XApiSharp.IntegrationTests/` | Explicitly-enabled real requests against X (disabled by default) |
| `tests/XApiSharp.PackageTests/` | Verifies the packed `.nupkg` output |
| `samples/` | Compilable sample applications |
| `spec/` | OpenAPI snapshot, operation manifest, documented overrides |
| `docs/` | Guides, ADRs, coverage matrix, release/maintenance runbooks |
| `eng/` | Build/pack/release scripts |
| `.github/workflows/` | CI and release automation |

## X Chat

`XApiClient.Chat` covers X Chat's documented v2 HTTP operations (conversations, messages, keys,
media) as typed transport contracts only. **This is not support for a full encrypted messenger.**
No key generation, encryption, decryption, or cryptographic state management happens in this SDK -
every key, ciphertext, and signature field is carried through exactly as the API declares it, for
your own end-to-end encryption implementation to produce and consume. For a complete encrypted
client, see X's own [Chat XDK](https://github.com/xdevplatform/chat-xdk).

Regular Direct Messages (`XApiClient.DirectMessages`) and X Chat are deliberately separate models
in this SDK, not merged into one message type - they are different products with different
guarantees.

## Contributing

See `CONTRIBUTING.md`. Security issues: see `SECURITY.md`.

## License

MIT for original code in this repository — see `LICENSE`. Third-party specification material
(the X OpenAPI document, documentation excerpts) is used and attributed under its own terms, not
relicensed as MIT.

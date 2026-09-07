# XApiSharp.PackageTests

Verifies the *packed* `.nupkg`/`.snupkg` output (spec sections 19.1 and 22.3): installs the built
package into a project outside the solution (no `ProjectReference` to SDK sources) and exercises
the public API against a test `HttpMessageHandler`.

**E2 status:** verified manually, not yet automated in this project. A temp console project
outside the repo (`dotnet add package XApiSharp.Net --source <local pack output>`) ran the read,
error, and cancellation scenarios against the installed 0.1.0-alpha.1 package successfully.

Not yet a standing automated project here: it needs to be a temporary project created by the
release tooling in `eng/` (so it can never accidentally gain a `ProjectReference` to SDK sources
by living in-solution) - that automation lands with the release-candidate pipeline in E8, per
`docs/roadmap.md`.

# XApiSharp.PackageTests

Verifies the *packed* `.nupkg`/`.snupkg` output (spec sections 19.1 and 22.3): installs the built
package into a project outside the solution (no `ProjectReference` to SDK sources) and exercises
the public API against a test `HttpMessageHandler`.

Not implemented yet — there is no package to test until stage E2 produces an installable local
package. This project is intentionally not a standard in-solution csproj so it can never
accidentally reference SDK sources directly; it will be scaffolded as a temporary project created
by the release tooling in `eng/`.

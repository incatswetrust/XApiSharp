namespace XApiSharp.CodeGen;

internal sealed class CliOptions
{
    public required string Command { get; init; }
    public required string SnapshotPath { get; init; }
    public required string GuardPath { get; init; }
    public required string ManifestPath { get; init; }
    public required string MethodMapPath { get; init; }
    public string OutputPath { get; init; } = "artifacts/codegen/generated-client.cs";
    public string ReportPath { get; init; } = "spec/generation-report.json";
    public string Namespace { get; init; } = "XApiSharp.Generated";
    public string ClassName { get; init; } = "XApiGeneratedClient";
    public string UnitCoveragePath { get; init; } = "";
    public string ContractCoveragePath { get; init; } = "";
    public double BranchCoverageThreshold { get; init; } = 80.0;
    public string ArtifactsDir { get; init; } = "artifacts";
    public string HashOutputPath { get; init; } = "artifacts/SHA256SUMS.txt";
    public string ReleaseManifestOutputPath { get; init; } = "artifacts/release-manifest.json";
    public string ReleaseVersion { get; init; } = "";
    public string Commit { get; init; } = "";
    public string? Tag { get; init; }
    public string? CiRunUrl { get; init; }
    public double BranchCoveragePercent { get; init; }
    public string RepoRoot { get; init; } = ".";
    public string SumsPath { get; init; } = "";
    public string ReleaseManifestPath { get; init; } = "";
    public string ExpectedVersion { get; init; } = "";

    private static readonly string[] KnownCommands =
    [
        "guard-check", "generate", "coverage-check", "branch-coverage-check",
        "hash-artifacts", "release-manifest", "verify-artifacts",
    ];

    public static CliOptions? Parse(string[] args)
    {
        if (args.Length == 0 || Array.IndexOf(KnownCommands, args[0]) < 0)
        {
            return null;
        }

        var command = args[0];
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 1; i < args.Length - 1; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                return null;
            }

            map[args[i][2..]] = args[i + 1];
        }

        return new CliOptions
        {
            Command = command,
            SnapshotPath = map.GetValueOrDefault("snapshot", "spec/openapi-snapshot.json"),
            GuardPath = map.GetValueOrDefault("guard", "spec/overrides/generation-guard.json"),
            ManifestPath = map.GetValueOrDefault("manifest", "spec/endpoint-manifest.json"),
            MethodMapPath = map.GetValueOrDefault("method-map", "spec/method-name-map.json"),
            OutputPath = map.GetValueOrDefault("output", "artifacts/codegen/generated-client.cs"),
            ReportPath = map.GetValueOrDefault("report", "spec/generation-report.json"),
            Namespace = map.GetValueOrDefault("namespace", "XApiSharp.Generated"),
            ClassName = map.GetValueOrDefault("classname", "XApiGeneratedClient"),
            UnitCoveragePath = map.GetValueOrDefault("unit", ""),
            ContractCoveragePath = map.GetValueOrDefault("contract", ""),
            BranchCoverageThreshold = double.TryParse(map.GetValueOrDefault("threshold"), out var t) ? t : 80.0,
            ArtifactsDir = map.GetValueOrDefault("dir", "artifacts"),
            HashOutputPath = map.GetValueOrDefault("output", "artifacts/SHA256SUMS.txt"),
            ReleaseManifestOutputPath = map.GetValueOrDefault("output", "artifacts/release-manifest.json"),
            ReleaseVersion = map.GetValueOrDefault("version", ""),
            Commit = map.GetValueOrDefault("commit", ""),
            Tag = map.GetValueOrDefault("tag"),
            CiRunUrl = map.GetValueOrDefault("ci-run-url"),
            BranchCoveragePercent = double.TryParse(map.GetValueOrDefault("branch-coverage-percent"), out var bc) ? bc : 0.0,
            RepoRoot = map.GetValueOrDefault("repo-root", "."),
            SumsPath = map.GetValueOrDefault("sums", "artifacts/SHA256SUMS.txt"),
            ReleaseManifestPath = map.GetValueOrDefault("release-manifest", "artifacts/release-manifest.json"),
            ExpectedVersion = map.GetValueOrDefault("expected-version", ""),
        };
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("""
            Usage:
              dotnet run --project tools/XApiSharp.CodeGen -- guard-check [--snapshot PATH] [--guard PATH] [--manifest PATH] [--method-map PATH]
              dotnet run --project tools/XApiSharp.CodeGen -- generate [--snapshot PATH] [--guard PATH] [--manifest PATH] [--method-map PATH] [--output PATH] [--report PATH] [--namespace NS] [--classname NAME]
              dotnet run --project tools/XApiSharp.CodeGen -- coverage-check [--manifest PATH]
              dotnet run --project tools/XApiSharp.CodeGen -- branch-coverage-check --unit PATH --contract PATH [--threshold 80]
              dotnet run --project tools/XApiSharp.CodeGen -- hash-artifacts --dir PATH [--output PATH]
              dotnet run --project tools/XApiSharp.CodeGen -- release-manifest --version V --commit SHA [--tag T] [--ci-run-url URL] [--branch-coverage-percent N] [--output PATH] [--repo-root PATH]
              dotnet run --project tools/XApiSharp.CodeGen -- verify-artifacts --dir PATH --sums PATH --release-manifest PATH --expected-version V

            guard-check: GEN-05 scan only (no NSwag invocation) - fails if the snapshot has an
              unregistered union schema or multipart operation. Also assigns GEN-09 stable method
              names for any new operations. Safe to run in CI on every PR.

            generate: runs guard-check, then invokes the pinned NSwag tool and post-processes its
              output (GEN-04 banner, GEN-07 report). Requires `dotnet tool restore` first.

            coverage-check: spec 22.1/27's coverage-matrix gate - fails if any in-scope operation
              in the endpoint manifest lacks a typed implementation or a contract test. Does not
              touch NSwag or the guard registry. Safe to run in CI on every PR.

            branch-coverage-check: spec 19.5's 80% hand-written-core branch-coverage gate. --unit/
              --contract point at the two projects' coverage.cobertura.xml (from
              `dotnet test --collect:"XPlat Code Coverage"`); merged by taking the max coverage per
              branch condition across both. Fails below --threshold (default 80).

            hash-artifacts: spec 22.2's "SHA-256 of the prepared artifacts" - hashes every
              *.nupkg/*.snupkg directly under --dir into a sha256sum-compatible manifest.

            release-manifest: spec 22.2's release-manifest.json (version, commit, tag, API
              snapshot, SDK/generator versions, coverage matrix, CI run link).

            verify-artifacts: spec 23.2 step 4 - re-checks a downloaded artifact set (from a
              specific release-candidate run) before publish: every file's hash matches --sums,
              and --release-manifest's recorded version matches --expected-version. Never
              rebuilds anything - this is a pure integrity check on already-produced artifacts.
            """);
    }
}

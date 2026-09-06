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

    public static CliOptions? Parse(string[] args)
    {
        if (args.Length == 0 || (args[0] != "guard-check" && args[0] != "generate"))
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
        };
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("""
            Usage:
              dotnet run --project tools/XApiSharp.CodeGen -- guard-check [--snapshot PATH] [--guard PATH] [--manifest PATH] [--method-map PATH]
              dotnet run --project tools/XApiSharp.CodeGen -- generate [--snapshot PATH] [--guard PATH] [--manifest PATH] [--method-map PATH] [--output PATH] [--report PATH] [--namespace NS] [--classname NAME]

            guard-check: GEN-05 scan only (no NSwag invocation) - fails if the snapshot has an
              unregistered union schema or multipart operation. Also assigns GEN-09 stable method
              names for any new operations. Safe to run in CI on every PR.

            generate: runs guard-check, then invokes the pinned NSwag tool and post-processes its
              output (GEN-04 banner, GEN-07 report). Requires `dotnet tool restore` first.
            """);
    }
}

using System.Text.Json;
using System.Text.Json.Nodes;

namespace XApiSharp.CodeGen;

/// <summary>
/// Spec 22.2's release-candidate output: "version, commit, tag, снимок API, версии SDK/генератора,
/// ссылки на CI и отчёты" (version, commit, tag, API snapshot, SDK/generator versions, links to
/// CI and reports) plus the coverage matrix. Reads the static, already-committed sources of truth
/// (<c>spec/spec-manifest.json</c>, <c>global.json</c>, <c>.config/dotnet-tools.json</c>,
/// <c>spec/endpoint-manifest.json</c>) rather than duplicating them - only the per-run values
/// (commit, tag, CI links, measured branch coverage) come in as arguments.
/// </summary>
internal static class ReleaseManifestWriter
{
    public sealed record Inputs(
        string Version,
        string Commit,
        string? Tag,
        string? CiRunUrl,
        double BranchCoveragePercent);

    public static void Write(string outputPath, Inputs inputs, string repoRoot)
    {
        var specManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(repoRoot, "spec/spec-manifest.json")))!.AsObject();
        var globalJson = JsonNode.Parse(File.ReadAllText(Path.Combine(repoRoot, "global.json")))!.AsObject();
        var toolsJson = JsonNode.Parse(File.ReadAllText(Path.Combine(repoRoot, ".config/dotnet-tools.json")))!.AsObject();
        var endpointManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(repoRoot, "spec/endpoint-manifest.json")))!.AsObject();

        var operations = endpointManifest["operations"]!.AsArray();
        var totalOps = operations.Count;
        var implemented = operations.Count(op => (string?)op!["implementation"] == "implemented");
        var contractTested = operations.Count(op => op!["contractTests"]?.AsArray().Count > 0);
        var liveValidated = operations.Count(op => (string?)op!["liveValidation"]!["status"] == "passed");

        var nswagVersion = (string?)toolsJson["tools"]?["nswag.consolecore"]?["version"];

        var manifest = new JsonObject
        {
            ["version"] = inputs.Version,
            ["commit"] = inputs.Commit,
            ["tag"] = inputs.Tag,
            ["generatedAtUtc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["ciRunUrl"] = inputs.CiRunUrl,
            ["sdk"] = new JsonObject
            {
                ["version"] = (string?)globalJson["sdk"]?["version"],
                ["rollForward"] = (string?)globalJson["sdk"]?["rollForward"],
            },
            ["generator"] = new JsonObject
            {
                ["tool"] = "nswag.consolecore",
                ["version"] = nswagVersion,
                ["note"] = "Pinned but not wired into committed source - see docs/adr/0002-code-generation.md; all shipped endpoint/DTO code in this release is hand-written.",
            },
            ["apiSnapshot"] = new JsonObject
            {
                ["sourceUrl"] = (string?)specManifest["sourceUrl"],
                ["retrievedAtUtc"] = (string?)specManifest["retrievedAtUtc"],
                ["sha256"] = (string?)specManifest["sha256"],
                ["upstreamInfoVersion"] = (string?)specManifest["upstreamInfoVersion"],
            },
            ["coverage"] = new JsonObject
            {
                ["totalOperations"] = totalOps,
                ["implemented"] = implemented,
                ["contractTested"] = contractTested,
                ["liveValidated"] = liveValidated,
                ["branchCoveragePercentCore"] = Math.Round(inputs.BranchCoveragePercent, 1),
            },
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}

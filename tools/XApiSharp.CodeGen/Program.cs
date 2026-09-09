// Dev-time CLI: wraps NSwag (spec/docs/adr/0002-code-generation.md) with the GEN-05/GEN-06
// generation guards, GEN-09 stable method-name assignment, and GEN-07 reporting this project
// requires but NSwag does not provide on its own. Not shipped as a NuGet dependency to SDK
// consumers (spec section 6.1).
using System.Globalization;
using System.Text.Json.Nodes;
using XApiSharp.CodeGen;

// This is CI/console output, not localized UI - force invariant formatting (e.g. "80.0%", never
// a runner-locale-dependent "80,0%") regardless of the host machine's culture.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var options = CliOptions.Parse(args);
if (options is null)
{
    CliOptions.PrintUsage();
    return 1;
}

if (options.Command == "hash-artifacts")
{
    ArtifactHasher.HashPackagesTo(options.ArtifactsDir, options.HashOutputPath);
    Console.WriteLine($"Hashed packages under '{options.ArtifactsDir}' to '{options.HashOutputPath}'.");
    Console.WriteLine(File.ReadAllText(options.HashOutputPath));
    return 0;
}

if (options.Command == "release-manifest")
{
    if (string.IsNullOrEmpty(options.ReleaseVersion) || string.IsNullOrEmpty(options.Commit))
    {
        Console.Error.WriteLine("release-manifest requires --version V and --commit SHA.");
        return 1;
    }

    ReleaseManifestWriter.Write(
        options.ReleaseManifestOutputPath,
        new ReleaseManifestWriter.Inputs(options.ReleaseVersion, options.Commit, options.Tag, options.CiRunUrl, options.BranchCoveragePercent),
        options.RepoRoot);
    Console.WriteLine($"Wrote release manifest to '{options.ReleaseManifestOutputPath}'.");
    return 0;
}

if (options.Command == "verify-artifacts")
{
    if (string.IsNullOrEmpty(options.ExpectedVersion))
    {
        Console.Error.WriteLine("verify-artifacts requires --expected-version V.");
        return 1;
    }

    var problems = new List<string>(ArtifactHasher.VerifyAgainstSums(options.ArtifactsDir, options.SumsPath));

    if (!File.Exists(options.ReleaseManifestPath))
    {
        problems.Add($"Release manifest not found: {options.ReleaseManifestPath}");
    }
    else
    {
        var releaseManifest = JsonNode.Parse(File.ReadAllText(options.ReleaseManifestPath))!.AsObject();
        var manifestVersion = (string?)releaseManifest["version"];
        if (manifestVersion != options.ExpectedVersion)
        {
            problems.Add($"Release manifest version '{manifestVersion}' does not match expected version '{options.ExpectedVersion}'.");
        }
    }

    if (problems.Count > 0)
    {
        Console.Error.WriteLine("ARTIFACT VERIFICATION FAILURE:");
        foreach (var problem in problems)
        {
            Console.Error.WriteLine($"  {problem}");
        }

        return 1;
    }

    Console.WriteLine($"verify-artifacts passed: hashes match and version '{options.ExpectedVersion}' confirmed in the release manifest.");
    return 0;
}

if (options.Command == "branch-coverage-check")
{
    if (string.IsNullOrEmpty(options.UnitCoveragePath) || string.IsNullOrEmpty(options.ContractCoveragePath))
    {
        Console.Error.WriteLine("branch-coverage-check requires --unit PATH and --contract PATH.");
        return 1;
    }

    string unitFile, contractFile;
    try
    {
        unitFile = ResolveCoberturaFile(options.UnitCoveragePath);
        contractFile = ResolveCoberturaFile(options.ContractCoveragePath);
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine(ex.Message);
        return 1;
    }

    var result = BranchCoverageChecker.Compute([unitFile, contractFile]);

    foreach (var file in result.Files)
    {
        Console.WriteLine($"  {file.File}: {file.Percent:F1}% ({file.Covered}/{file.Total})");
    }

    Console.WriteLine($"Total (core): {result.TotalPercent:F1}% ({result.TotalCovered}/{result.TotalUnits})");

    if (result.TotalPercent < options.BranchCoverageThreshold)
    {
        Console.Error.WriteLine($"BRANCH COVERAGE GATE FAILURE: {result.TotalPercent:F1}% is below the {options.BranchCoverageThreshold:F0}% threshold (spec 19.5).");
        return 1;
    }

    Console.WriteLine($"branch-coverage-check passed: {result.TotalPercent:F1}% >= {options.BranchCoverageThreshold:F0}%.");
    return 0;
}

if (options.Command == "coverage-check")
{
    var manifestForCoverage = JsonNode.Parse(File.ReadAllText(options.ManifestPath))!.AsObject();
    var problems = EndpointCoverageChecker.Check(manifestForCoverage);

    if (problems.Count > 0)
    {
        Console.Error.WriteLine("COVERAGE GATE FAILURE: the following in-scope operations are not fully covered:");
        foreach (var problem in problems)
        {
            Console.Error.WriteLine($"  {problem}");
        }

        return 1;
    }

    Console.WriteLine("coverage-check passed: every in-scope operation has a typed implementation and at least one contract test.");
    return 0;
}

var snapshot = JsonNode.Parse(File.ReadAllText(options.SnapshotPath))!.AsObject();
var guard = GuardRegistry.Load(options.GuardPath);

var unionFindings = SnapshotScanner.FindUnionSchemas(snapshot);
var multipartFindings = SnapshotScanner.FindMultipartOperations(snapshot);

var unresolvedUnions = unionFindings.Where(f => !guard.IsUnionRegistered(f.SchemaName)).ToList();
var unresolvedMultipart = multipartFindings.Where(f => !guard.IsMultipartRegistered(f.Key)).ToList();

Console.WriteLine($"Scanned snapshot: {unionFindings.Count} union schema(s), {multipartFindings.Count} multipart-with-alternate-content-type operation(s).");
Console.WriteLine($"Guard registry covers {unionFindings.Count - unresolvedUnions.Count}/{unionFindings.Count} union schemas and {multipartFindings.Count - unresolvedMultipart.Count}/{multipartFindings.Count} multipart operations.");

if (unresolvedUnions.Count > 0 || unresolvedMultipart.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("GEN-05 FAILURE: the snapshot contains constructs known to defeat the generator that are NOT registered in the guard file (spec/overrides/generation-guard.json).");
    Console.Error.WriteLine("Add a documented entry (schema/operation, reason, source, status) before proceeding - do not let generation silently produce an empty class or drop a request body.");

    foreach (var u in unresolvedUnions)
    {
        Console.Error.WriteLine($"  UNREGISTERED union schema: {u.SchemaName} ({u.Branches} branches, discriminator={u.HasDiscriminator})");
    }

    foreach (var m in unresolvedMultipart)
    {
        Console.Error.WriteLine($"  UNREGISTERED multipart operation: {m.Key} (content-types: {string.Join(", ", m.ContentTypes)})");
    }

    return 1;
}

// GEN-09: assign stable method names for any newly discovered in-scope operation.
var methodMap = MethodNameMap.Load(options.MethodMapPath);
var manifest = JsonNode.Parse(File.ReadAllText(options.ManifestPath))!.AsObject();
var operations = manifest["operations"]!.AsArray();

foreach (var opNode in operations)
{
    var op = opNode!.AsObject();
    var key = op["key"]!.GetValue<string>();
    var group = op["group"]!.GetValue<string>();
    var method = op["method"]!.GetValue<string>();
    var path = op["path"]!.GetValue<string>();
    methodMap.GetOrAssign(key, group, method, path);
}

var collisionWarnings = methodMap.ResolveCollisions();
foreach (var warning in collisionWarnings)
{
    Console.Error.WriteLine($"GEN-09 WARNING: {warning}");
}

if (methodMap.NewlyAssigned.Count > 0)
{
    Console.WriteLine($"GEN-09: assigned {methodMap.NewlyAssigned.Count} new stable method name(s) in {options.MethodMapPath}.");
    methodMap.Save(options.MethodMapPath);
}
else
{
    Console.WriteLine("GEN-09: no new operations since the last method-name-map run.");
}

if (options.Command == "guard-check")
{
    Console.WriteLine("guard-check passed.");
    return 0;
}

// options.Command == "generate": invoke the pinned NSwag tool, then post-process its output.
Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);

var nswagExitCode = NSwagInvoker.Run(options.SnapshotPath, options.OutputPath, options.Namespace, options.ClassName);
if (nswagExitCode != 0)
{
    Console.Error.WriteLine($"GEN-01 FAILURE: nswag exited with code {nswagExitCode}.");
    return nswagExitCode;
}

GeneratedFileStamper.Stamp(options.OutputPath, options.SnapshotPath);

GenerationReport.Write(
    options.ReportPath,
    snapshot,
    unionFindings,
    multipartFindings,
    operations.Count,
    options.OutputPath);

Console.WriteLine($"Generation complete. Output: {options.OutputPath}. Report: {options.ReportPath}.");
return 0;

// Accepts either a direct file path or a directory to search recursively - `dotnet test
// --collect:"XPlat Code Coverage" --results-directory <dir>` names its output
// `<dir>/<run-guid>/coverage.cobertura.xml`, and the exact guid isn't known ahead of time. Doing
// the search here (portable dotnet code) avoids relying on OS-specific find/glob syntax in CI
// YAML, which would otherwise differ between the Linux/Windows/macOS runners spec 22.2 requires.
static string ResolveCoberturaFile(string path)
{
    if (File.Exists(path))
    {
        return path;
    }

    if (Directory.Exists(path))
    {
        var found = Directory.EnumerateFiles(path, "coverage.cobertura.xml", SearchOption.AllDirectories).ToList();
        if (found.Count == 1)
        {
            return found[0];
        }

        if (found.Count > 1)
        {
            throw new FileNotFoundException($"Found {found.Count} coverage.cobertura.xml files under '{path}' - expected exactly one. Pass the exact file path instead.");
        }
    }

    throw new FileNotFoundException($"No coverage.cobertura.xml found at or under '{path}'.");
}

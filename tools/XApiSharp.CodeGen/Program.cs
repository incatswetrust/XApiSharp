// Dev-time CLI: wraps NSwag (spec/docs/adr/0002-code-generation.md) with the GEN-05/GEN-06
// generation guards, GEN-09 stable method-name assignment, and GEN-07 reporting this project
// requires but NSwag does not provide on its own. Not shipped as a NuGet dependency to SDK
// consumers (spec section 6.1).
using System.Text.Json.Nodes;
using XApiSharp.CodeGen;

var options = CliOptions.Parse(args);
if (options is null)
{
    CliOptions.PrintUsage();
    return 1;
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

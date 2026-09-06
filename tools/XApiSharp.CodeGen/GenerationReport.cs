using System.Text.Json.Nodes;

namespace XApiSharp.CodeGen;

/// <summary>
/// GEN-07: generator report covering operations, models, and the guard findings, so a human can
/// review what was generated / excluded without re-deriving it from the raw snapshot.
/// </summary>
internal static class GenerationReport
{
    public static void Write(
        string reportPath,
        JsonObject snapshot,
        IReadOnlyList<UnionSchemaFinding> unionFindings,
        IReadOnlyList<MultipartOperationFinding> multipartFindings,
        int totalOperations,
        string outputPath)
    {
        var schemaCount = (snapshot["components"]?["schemas"] as JsonObject)?.Count ?? 0;

        var report = new JsonObject
        {
            ["generatedAtUtc"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
            ["totalOperationsInManifest"] = totalOperations,
            ["totalSchemasInSnapshot"] = schemaCount,
            ["unionSchemas"] = new JsonObject
            {
                ["found"] = unionFindings.Count,
                ["names"] = new JsonArray(unionFindings.Select(f => (JsonNode)f.SchemaName).ToArray()),
            },
            ["multipartOperations"] = new JsonObject
            {
                ["found"] = multipartFindings.Count,
                ["keys"] = new JsonArray(multipartFindings.Select(f => (JsonNode)f.Key).ToArray()),
            },
            ["outputFile"] = outputPath,
            ["outputFileSha256"] = File.Exists(outputPath) ? FileHash.Sha256(outputPath) : null,
        };

        File.WriteAllText(reportPath, report.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}

using System.Text.Json.Nodes;

namespace XApiSharp.CodeGen;

internal sealed record UnionSchemaFinding(string SchemaName, int Branches, bool HasDiscriminator);

internal sealed record MultipartOperationFinding(string Key, IReadOnlyList<string> ContentTypes);

/// <summary>
/// Scans the raw OpenAPI snapshot for the two construct classes known (per ADR 0002) to defeat
/// the generator: top-level oneOf/anyOf schemas, and operations whose requestBody declares
/// multipart/form-data alongside another content type.
/// </summary>
internal static class SnapshotScanner
{
    public static IReadOnlyList<UnionSchemaFinding> FindUnionSchemas(JsonObject snapshot)
    {
        var findings = new List<UnionSchemaFinding>();
        if (snapshot["components"]?["schemas"] is not JsonObject schemas)
        {
            return findings;
        }

        foreach (var (name, node) in schemas)
        {
            if (node is not JsonObject schema)
            {
                continue;
            }

            var union = schema["oneOf"] as JsonArray ?? schema["anyOf"] as JsonArray;
            if (union is null)
            {
                continue;
            }

            findings.Add(new UnionSchemaFinding(name, union.Count, schema["discriminator"] is not null));
        }

        return findings.OrderBy(f => f.SchemaName, StringComparer.Ordinal).ToList();
    }

    public static IReadOnlyList<MultipartOperationFinding> FindMultipartOperations(JsonObject snapshot)
    {
        var findings = new List<MultipartOperationFinding>();
        if (snapshot["paths"] is not JsonObject paths)
        {
            return findings;
        }

        string[] methods = ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

        foreach (var (path, pathItemNode) in paths)
        {
            if (pathItemNode is not JsonObject pathItem)
            {
                continue;
            }

            foreach (var method in methods)
            {
                if (pathItem[method] is not JsonObject op)
                {
                    continue;
                }

                if (op["requestBody"]?["content"] is not JsonObject content)
                {
                    continue;
                }

                var contentTypes = content.Select(kv => kv.Key).ToList();
                if (contentTypes.Contains("multipart/form-data") && contentTypes.Count > 1)
                {
                    findings.Add(new MultipartOperationFinding($"{method.ToUpperInvariant()} {path}", contentTypes));
                }
            }
        }

        return findings.OrderBy(f => f.Key, StringComparer.Ordinal).ToList();
    }
}

using System.Text.Json.Nodes;

namespace XApiSharp.CodeGen;

/// <summary>
/// GEN-05/GEN-06: documented, sourced list of snapshot constructs known to defeat the
/// generator (spec/overrides/generation-guard.json). Anything matching these constructs that
/// is NOT registered here fails generation instead of being silently accepted.
/// </summary>
internal sealed class GuardRegistry
{
    private readonly HashSet<string> _unionSchemas;
    private readonly HashSet<string> _multipartOperationKeys;

    private GuardRegistry(HashSet<string> unionSchemas, HashSet<string> multipartOperationKeys)
    {
        _unionSchemas = unionSchemas;
        _multipartOperationKeys = multipartOperationKeys;
    }

    public bool IsUnionRegistered(string schemaName) => _unionSchemas.Contains(schemaName);

    public bool IsMultipartRegistered(string operationKey) => _multipartOperationKeys.Contains(operationKey);

    public static GuardRegistry Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Guard registry not found: {path}");
        }

        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

        var unions = new HashSet<string>(StringComparer.Ordinal);
        if (root["unionSchemas"] is JsonArray unionArray)
        {
            foreach (var entry in unionArray)
            {
                var name = entry!["schema"]!.GetValue<string>();
                unions.Add(name);
            }
        }

        var multipart = new HashSet<string>(StringComparer.Ordinal);
        if (root["multipartOperations"] is JsonArray multipartArray)
        {
            foreach (var entry in multipartArray)
            {
                var key = entry!["key"]!.GetValue<string>();
                multipart.Add(key);
            }
        }

        return new GuardRegistry(unions, multipart);
    }
}

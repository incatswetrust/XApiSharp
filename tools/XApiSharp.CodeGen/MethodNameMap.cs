using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace XApiSharp.CodeGen;

/// <summary>
/// GEN-09: stable public method names, decoupled from upstream operationId. Once an operation
/// gets an entry here, renaming/reshuffling operationId upstream must not change the assigned
/// C# method name - existing entries are never overwritten, only new ones are appended.
/// </summary>
internal sealed class MethodNameMap
{
    private readonly Dictionary<string, string> _entries;
    private readonly List<string> _newlyAssigned = [];

    private MethodNameMap(Dictionary<string, string> entries)
    {
        _entries = entries;
    }

    public IReadOnlyList<string> NewlyAssigned => _newlyAssigned;

    public static MethodNameMap Load(string path)
    {
        if (!File.Exists(path))
        {
            return new MethodNameMap(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, node) in root)
        {
            entries[key] = node!["methodName"]!.GetValue<string>();
        }

        return new MethodNameMap(entries);
    }

    public string GetOrAssign(string operationKey, string group, string method, string path)
    {
        if (_entries.TryGetValue(operationKey, out var existing))
        {
            return existing;
        }

        var assigned = DeriveName(group, method, path);
        _entries[operationKey] = assigned;
        _newlyAssigned.Add(operationKey);
        return assigned;
    }

    /// <summary>
    /// Must run after every GetOrAssign call for the current batch, before Save. Two different
    /// operations deriving the same name (e.g. a collection endpoint and a same-shaped nested
    /// endpoint) is a real defect, not a corner case to ignore - resolved by appending a stable
    /// numeric suffix, with a warning printed so a human reviews/renames it in the committed file.
    /// </summary>
    public IReadOnlyList<string> ResolveCollisions()
    {
        var warnings = new List<string>();
        var seen = new Dictionary<string, string>(StringComparer.Ordinal); // name -> first operationKey that claimed it

        foreach (var (opKey, name) in _entries.OrderBy(e => e.Key, StringComparer.Ordinal).ToList())
        {
            if (!seen.TryGetValue(name, out var firstOwner))
            {
                seen[name] = opKey;
                continue;
            }

            if (firstOwner == opKey)
            {
                continue;
            }

            // Only rename entries assigned in this run - never silently rewrite a previously
            // stable name just because a new operation collided with it.
            if (!_newlyAssigned.Contains(opKey))
            {
                warnings.Add($"COLLISION (unresolved, pre-existing entry kept stable): '{name}' is used by both '{firstOwner}' and '{opKey}'. Rename one manually in method-name-map.json.");
                continue;
            }

            var suffix = 2;
            string candidate;
            do
            {
                candidate = $"{name}_{suffix}";
                suffix++;
            }
            while (seen.ContainsKey(candidate));

            _entries[opKey] = candidate;
            seen[candidate] = opKey;
            warnings.Add($"COLLISION resolved by suffix: '{opKey}' renamed from '{name}' to '{candidate}'. Review and give it a proper distinct name in method-name-map.json.");
        }

        return warnings;
    }

    public void Save(string path)
    {
        var root = new JsonObject();
        foreach (var (key, name) in _entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            root[key] = new JsonObject
            {
                ["methodName"] = name,
            };
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, root.ToJsonString(options) + "\n");
    }

    /// <summary>
    /// Derives a first-draft stable name of the form "Group.VerbNounAsync" from the HTTP method
    /// and path shape, folding path parameters in as "By&lt;Param&gt;" at the point they occur
    /// rather than dropping them (dropping them is what caused same-shaped collection/by-id pairs
    /// to collide). This is a starting point only - names assigned here can be hand-edited in
    /// method-name-map.json afterwards; once present in the file they are never regenerated.
    /// </summary>
    private static string DeriveName(string group, string method, string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s != "2")
            .ToList();

        var verb = method.ToUpperInvariant() switch
        {
            "GET" when path.Contains('{') => "Get",
            "GET" => "List",
            "POST" => "Create",
            "PUT" => "Update",
            "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => "Invoke",
        };

        var nounBuilder = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.StartsWith('{') && segment.EndsWith('}'))
            {
                var paramName = segment[1..^1];
                nounBuilder.Append("By").Append(ToPascalCase(paramName));
            }
            else
            {
                nounBuilder.Append(ToPascalCase(segment));
            }
        }

        var noun = nounBuilder.Length > 0 ? nounBuilder.ToString() : "Root";
        var groupPascal = ToPascalCase(group.Replace(' ', '_'));
        return $"{groupPascal}.{verb}{noun}Async";
    }

    private static string ToPascalCase(string segment)
    {
        var parts = segment.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                continue;
            }

            sb.Append(char.ToUpper(part[0], CultureInfo.InvariantCulture));
            if (part.Length > 1)
            {
                sb.Append(part[1..]);
            }
        }

        return sb.ToString();
    }
}

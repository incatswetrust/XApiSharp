using System.Text;

namespace XApiSharp.Transport;

/// <summary>
/// SER-10: correct query-string construction - each value is escaped exactly once
/// (<see cref="Uri.EscapeDataString(string)"/>, never applied to an already-escaped path/query),
/// and array-shaped parameters use X's documented comma-separated convention (e.g.
/// <c>user.fields=id,name,username</c>) rather than repeated keys. No current request model has
/// optional query parameters yet (E4 adds `fields`/`expansions`/pagination) - this is the shared
/// primitive they will all build on, so it isn't duplicated per endpoint.
/// </summary>
internal static class QueryStringBuilder
{
    /// <summary>Builds a leading-<c>?</c> query string from name/value pairs, skipping any
    /// entry whose value is null or empty. Returns <c>null</c> (not <c>"?"</c>) when every
    /// value is absent.</summary>
    public static string? Build(IReadOnlyList<(string Name, string? Value)> parameters)
    {
        StringBuilder? builder = null;

        foreach (var (name, value) in parameters)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            builder ??= new StringBuilder("?");
            if (builder.Length > 1)
            {
                builder.Append('&');
            }

            builder.Append(Uri.EscapeDataString(name)).Append('=').Append(Uri.EscapeDataString(value));
        }

        return builder?.ToString();
    }

    /// <summary>X's convention for array-shaped query parameters: comma-joined, not repeated
    /// keys. Returns null for an empty/null collection so <see cref="Build"/> omits it.</summary>
    public static string? JoinCommaSeparated(IReadOnlyCollection<string>? values) =>
        values is null || values.Count == 0 ? null : string.Join(',', values);
}

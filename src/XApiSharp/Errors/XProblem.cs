using System.Text.Json.Serialization;

namespace XApiSharp.Errors;

/// <summary>
/// Hand-modeled equivalent of the X API "Problem" schema (RFC 7807-style: a 9-branch
/// discriminated <c>oneOf</c> in the snapshot). Registered in
/// spec/overrides/generation-guard.json - NSwag generates an empty class for this schema, so it
/// is modeled by hand instead of generated (see docs/adr/0002-code-generation.md).
///
/// Variant-specific fields (e.g. <c>resource_type</c>/<c>resource_id</c> on a "resource not
/// found" problem, <c>parameter</c>/<c>value</c> on an "invalid request" problem) are exposed via
/// <see cref="ExtensionData"/> rather than nine separate C# subtypes - full per-variant typing is
/// deferred to E6 stabilization once error-handling tests exist to pin the shape down.
/// </summary>
public sealed class XProblem
{
    /// <summary>The problem type URI, e.g. <c>https://api.x.com/2/problems/resource-not-found</c>.</summary>
    public required string Type { get; init; }

    public required string Title { get; init; }

    public string? Detail { get; init; }

    public int? Status { get; init; }

    private Dictionary<string, System.Text.Json.JsonElement>? _extensionData;

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement> ExtensionData
    {
        get => _extensionData ??= [];
        set => _extensionData = value;
    }
}

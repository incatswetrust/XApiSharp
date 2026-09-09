using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Activity;

/// <summary>
/// Modeled from the "Filter" shape the registry repeats identically (different generated schema
/// names) across the create request and the get/create/update responses - one shared type instead
/// of 4 near-identical classes.
/// </summary>
public sealed class ActivitySubscriptionFilter
{
    /// <summary>Not supported for <c>mute.*</c>/<c>block.*</c> events, per the registry.</summary>
    public string? Direction { get; init; }

    public string? Keyword { get; init; }

    /// <summary>SER-09/SER-12 escape hatch - event-specific string predicates, an untyped object
    /// per the registry.</summary>
    public JsonElement? Qualifiers { get; init; }

    /// <summary>User the subscription is scoped to. For <c>mute.*</c>/<c>block.*</c> events, must
    /// be the authenticated source user, per the registry.</summary>
    public string? UserId { get; init; }
}

internal sealed class ActivitySubscriptionFilterBody
{
    [JsonPropertyName("direction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Direction { get; init; }

    [JsonPropertyName("keyword")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Keyword { get; init; }

    [JsonPropertyName("qualifiers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Qualifiers { get; init; }

    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; init; }
}

/// <summary>Modeled from the response-side "Filter" shape (the same 4 fields, but every field is
/// also readable, not just write-only).</summary>
public sealed class ActivitySubscriptionFilterData
{
    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    [JsonPropertyName("keyword")]
    public string? Keyword { get; init; }

    [JsonPropertyName("qualifiers")]
    public JsonElement? Qualifiers { get; init; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}

internal static class ActivitySubscriptionFilterMapper
{
    public static ActivitySubscriptionFilterBody? ToBody(ActivitySubscriptionFilter? filter) =>
        filter is null
            ? null
            : new ActivitySubscriptionFilterBody
            {
                Direction = filter.Direction,
                Keyword = filter.Keyword,
                Qualifiers = filter.Qualifiers,
                UserId = filter.UserId,
            };
}

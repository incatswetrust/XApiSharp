using System.Text.Json.Serialization;

namespace XApiSharp.Trends;

/// <summary>Modeled from the "PersonalizedTrend" schema. <c>post_count</c> is a string in the
/// registry's own schema (not an integer, unlike <see cref="Trend.TweetCount"/>) - kept as-is
/// rather than parsed, since SER-01/SER-12 favor preserving the contract's actual declared shape
/// over a locally "corrected" type.</summary>
public sealed class PersonalizedTrend
{
    [JsonPropertyName("trend_name")]
    public string? TrendName { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("post_count")]
    public string? PostCount { get; init; }

    [JsonPropertyName("trending_since")]
    public string? TrendingSince { get; init; }
}

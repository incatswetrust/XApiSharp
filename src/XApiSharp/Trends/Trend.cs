using System.Text.Json.Serialization;

namespace XApiSharp.Trends;

/// <summary>Modeled from the "Trend" schema.</summary>
public sealed class Trend
{
    [JsonPropertyName("trend_name")]
    public string? TrendName { get; init; }

    [JsonPropertyName("tweet_count")]
    public long? TweetCount { get; init; }
}

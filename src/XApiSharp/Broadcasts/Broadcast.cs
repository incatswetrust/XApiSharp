using System.Text.Json.Serialization;

namespace XApiSharp.Broadcasts;

/// <summary>
/// Modeled from the "Broadcast" schema - an X Live broadcast. Undocumented on docs.x.com at E0
/// inventory time (present in the OpenAPI snapshot only); confirmed in scope for E4 per project
/// owner decision despite that risk. Most numeric-looking fields (<c>*_ms</c> timestamps,
/// <c>total_watched</c>/<c>total_watching</c>) are decimal strings in the registry's own schema,
/// not integers - preserved as strings rather than parsed (SER-01/SER-12), same treatment as
/// <c>Trends.PersonalizedTrend.PostCount</c>.
/// </summary>
public sealed class Broadcast
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("broadcast_id")]
    public string? BroadcastId { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("source_id")]
    public string? SourceId { get; init; }

    [JsonPropertyName("media_key")]
    public string? MediaKey { get; init; }

    [JsonPropertyName("tweet_id")]
    public string? TweetId { get; init; }

    [JsonPropertyName("twitter_user_id")]
    public string? TwitterUserId { get; init; }

    [JsonPropertyName("share_url")]
    public string? ShareUrl { get; init; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; init; }

    [JsonPropertyName("image_url_medium")]
    public string? ImageUrlMedium { get; init; }

    [JsonPropertyName("image_url_small")]
    public string? ImageUrlSmall { get; init; }

    [JsonPropertyName("chat_option")]
    public int? ChatOption { get; init; }

    [JsonPropertyName("available_for_replay")]
    public bool? AvailableForReplay { get; init; }

    [JsonPropertyName("is_high_latency")]
    public bool? IsHighLatency { get; init; }

    [JsonPropertyName("width")]
    public int? Width { get; init; }

    [JsonPropertyName("height")]
    public int? Height { get; init; }

    [JsonPropertyName("created_at_ms")]
    public string? CreatedAtMs { get; init; }

    [JsonPropertyName("updated_at_ms")]
    public string? UpdatedAtMs { get; init; }

    [JsonPropertyName("start_ms")]
    public string? StartMs { get; init; }

    [JsonPropertyName("end_ms")]
    public string? EndMs { get; init; }

    [JsonPropertyName("scheduled_start_ms")]
    public string? ScheduledStartMs { get; init; }

    [JsonPropertyName("scheduled_end_ms")]
    public string? ScheduledEndMs { get; init; }

    [JsonPropertyName("total_watched")]
    public string? TotalWatched { get; init; }

    [JsonPropertyName("total_watching")]
    public string? TotalWatching { get; init; }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>GET /2/media/analytics</c>.</summary>
public sealed class GetMediaAnalyticsRequest
{
    public required IReadOnlyCollection<string> MediaKeys { get; init; }

    public required DateTimeOffset StartTime { get; init; }

    public required DateTimeOffset EndTime { get; init; }

    public XMediaAnalyticsGranularity? Granularity { get; init; }

    public IReadOnlyCollection<XMediaAnalyticsField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetMediaAnalyticsResponse" schema.</summary>
public sealed class GetMediaAnalyticsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<MediaAnalytics>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

/// <summary>Modeled from the "MediaAnalytics" schema. <c>timestamped_metrics</c> stays
/// <see cref="JsonElement"/> (SER-09).</summary>
public sealed class MediaAnalytics
{
    [JsonPropertyName("media_key")]
    public string? MediaKey { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("video_views")]
    public long? VideoViews { get; init; }

    [JsonPropertyName("playback_start")]
    public long? PlaybackStart { get; init; }

    [JsonPropertyName("playback25")]
    public long? Playback25 { get; init; }

    [JsonPropertyName("playback50")]
    public long? Playback50 { get; init; }

    [JsonPropertyName("playback75")]
    public long? Playback75 { get; init; }

    [JsonPropertyName("playback_complete")]
    public long? PlaybackComplete { get; init; }

    [JsonPropertyName("play_from_tap")]
    public long? PlayFromTap { get; init; }

    [JsonPropertyName("watch_time_ms")]
    public long? WatchTimeMs { get; init; }

    [JsonPropertyName("cta_url_clicks")]
    public long? CtaUrlClicks { get; init; }

    [JsonPropertyName("cta_watch_clicks")]
    public long? CtaWatchClicks { get; init; }

    [JsonPropertyName("timestamped_metrics")]
    public JsonElement? TimestampedMetrics { get; init; }
}

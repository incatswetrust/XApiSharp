namespace XApiSharp.Common;

/// <summary>The <c>media_analytics.fields</c> query parameter (SER-05).</summary>
public enum XMediaAnalyticsField
{
    CtaUrlClicks,
    CtaWatchClicks,
    MediaKey,
    PlayFromTap,
    Playback25,
    Playback50,
    Playback75,
    PlaybackComplete,
    PlaybackStart,
    Timestamp,
    TimestampedMetrics,
    VideoViews,
    WatchTimeMs,
}

public static class XMediaAnalyticsFieldExtensions
{
    public static string ToApiValue(this XMediaAnalyticsField field) => field switch
    {
        XMediaAnalyticsField.CtaUrlClicks => "cta_url_clicks",
        XMediaAnalyticsField.CtaWatchClicks => "cta_watch_clicks",
        XMediaAnalyticsField.MediaKey => "media_key",
        XMediaAnalyticsField.PlayFromTap => "play_from_tap",
        XMediaAnalyticsField.Playback25 => "playback25",
        XMediaAnalyticsField.Playback50 => "playback50",
        XMediaAnalyticsField.Playback75 => "playback75",
        XMediaAnalyticsField.PlaybackComplete => "playback_complete",
        XMediaAnalyticsField.PlaybackStart => "playback_start",
        XMediaAnalyticsField.Timestamp => "timestamp",
        XMediaAnalyticsField.TimestampedMetrics => "timestamped_metrics",
        XMediaAnalyticsField.VideoViews => "video_views",
        XMediaAnalyticsField.WatchTimeMs => "watch_time_ms",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

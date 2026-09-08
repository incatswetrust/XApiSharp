namespace XApiSharp.Common;

/// <summary>The <c>broadcast.fields</c> query parameter (SER-05).</summary>
public enum XBroadcastField
{
    AvailableForReplay,
    BroadcastId,
    ChatOption,
    CreatedAtMs,
    EndMs,
    Height,
    Id,
    ImageUrl,
    ImageUrlMedium,
    ImageUrlSmall,
    IsHighLatency,
    Language,
    MediaKey,
    ScheduledEndMs,
    ScheduledStartMs,
    ShareUrl,
    SourceId,
    StartMs,
    State,
    Title,
    TotalWatched,
    TotalWatching,
    TweetId,
    TwitterUserId,
    UpdatedAtMs,
    Width,
}

public static class XBroadcastFieldExtensions
{
    public static string ToApiValue(this XBroadcastField field) => field switch
    {
        XBroadcastField.AvailableForReplay => "available_for_replay",
        XBroadcastField.BroadcastId => "broadcast_id",
        XBroadcastField.ChatOption => "chat_option",
        XBroadcastField.CreatedAtMs => "created_at_ms",
        XBroadcastField.EndMs => "end_ms",
        XBroadcastField.Height => "height",
        XBroadcastField.Id => "id",
        XBroadcastField.ImageUrl => "image_url",
        XBroadcastField.ImageUrlMedium => "image_url_medium",
        XBroadcastField.ImageUrlSmall => "image_url_small",
        XBroadcastField.IsHighLatency => "is_high_latency",
        XBroadcastField.Language => "language",
        XBroadcastField.MediaKey => "media_key",
        XBroadcastField.ScheduledEndMs => "scheduled_end_ms",
        XBroadcastField.ScheduledStartMs => "scheduled_start_ms",
        XBroadcastField.ShareUrl => "share_url",
        XBroadcastField.SourceId => "source_id",
        XBroadcastField.StartMs => "start_ms",
        XBroadcastField.State => "state",
        XBroadcastField.Title => "title",
        XBroadcastField.TotalWatched => "total_watched",
        XBroadcastField.TotalWatching => "total_watching",
        XBroadcastField.TweetId => "tweet_id",
        XBroadcastField.TwitterUserId => "twitter_user_id",
        XBroadcastField.UpdatedAtMs => "updated_at_ms",
        XBroadcastField.Width => "width",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

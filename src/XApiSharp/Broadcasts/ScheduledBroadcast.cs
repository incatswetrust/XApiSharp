using System.Text.Json.Serialization;

namespace XApiSharp.Broadcasts;

/// <summary>
/// Shared response shape for every scheduled-broadcast operation
/// (ListScheduledBroadcastsResponseData/CreateScheduledBroadcastResponseData/
/// GetScheduledBroadcastResponseData/UpdateScheduledBroadcastResponseData/
/// GoLiveScheduledBroadcastResponseData - all identical in the registry).
/// </summary>
public sealed class ScheduledBroadcast
{
    /// <summary>Numeric scheduler id. Required in the update request body.</summary>
    [JsonPropertyName("scheduled_broadcast_id")]
    public string? ScheduledBroadcastId { get; init; }

    /// <summary>Alphanumeric UBS broadcast id - the path <c>:id</c> for get/update/delete/live.</summary>
    [JsonPropertyName("broadcast_id")]
    public string? BroadcastId { get; init; }

    /// <summary>Scheduler state (Created, Scheduled, Running, ...).</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    [JsonPropertyName("source_id")]
    public string? SourceId { get; init; }

    [JsonPropertyName("chat_option")]
    public string? ChatOption { get; init; }

    [JsonPropertyName("telecast_id")]
    public string? TelecastId { get; init; }

    [JsonPropertyName("thumbnail_media_id")]
    public string? ThumbnailMediaId { get; init; }

    [JsonPropertyName("recurring_schedule_id")]
    public string? RecurringScheduleId { get; init; }

    [JsonPropertyName("available_for_replay")]
    public bool? AvailableForReplay { get; init; }

    /// <summary>When true, the coordinator will not auto-publish - call
    /// <see cref="BroadcastsClient.GoLiveScheduledAsync"/> when ready.</summary>
    [JsonPropertyName("manual_publish")]
    public bool? ManualPublish { get; init; }

    [JsonPropertyName("scheduled_start_ms")]
    public string? ScheduledStartMs { get; init; }

    [JsonPropertyName("scheduled_end_ms")]
    public string? ScheduledEndMs { get; init; }
}

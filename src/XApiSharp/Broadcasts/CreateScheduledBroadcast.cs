using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>POST /2/broadcasts/scheduled</c>.</summary>
public sealed class CreateScheduledBroadcastRequest
{
    public required string SourceId { get; init; }

    /// <summary>Milliseconds since Unix epoch, per the registry (a numeric string on the wire,
    /// exposed here as <see cref="DateTimeOffset"/> for ergonomics).</summary>
    public required DateTimeOffset ScheduledStart { get; init; }

    public required DateTimeOffset ScheduledEnd { get; init; }

    public string? Title { get; init; }

    public string? Locale { get; init; }

    public string? ChatOption { get; init; }

    public string? TelecastId { get; init; }

    public string? ThumbnailMediaId { get; init; }

    public bool? AvailableForReplay { get; init; }

    public bool? IsLocked { get; init; }

    /// <summary>When true, the coordinator will not auto-publish - call
    /// <see cref="BroadcastsClient.GoLiveScheduledAsync"/> when ready.</summary>
    public bool? ManualPublish { get; init; }

    public ScheduledBroadcastRecurrence? Recurrence { get; init; }
}

public sealed class ScheduledBroadcastRecurrence
{
    public required ScheduledBroadcastFrequency Frequency { get; init; }

    public required int Repeats { get; init; }
}

public enum ScheduledBroadcastFrequency
{
    Daily,
    Weekly,
}

internal static class ScheduledBroadcastFrequencyExtensions
{
    public static string ToApiValue(this ScheduledBroadcastFrequency value) => value switch
    {
        ScheduledBroadcastFrequency.Daily => "Daily",
        ScheduledBroadcastFrequency.Weekly => "Weekly",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

/// <summary>Modeled from the "CreateScheduledBroadcastResponse" schema. Returned with HTTP 201.</summary>
public sealed class CreateScheduledBroadcastResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ScheduledBroadcast? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class CreateScheduledBroadcastBody
{
    [JsonPropertyName("source_id")]
    public required string SourceId { get; init; }

    [JsonPropertyName("scheduled_start_ms")]
    public required string ScheduledStartMs { get; init; }

    [JsonPropertyName("scheduled_end_ms")]
    public required string ScheduledEndMs { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("locale")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Locale { get; init; }

    [JsonPropertyName("chat_option")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChatOption { get; init; }

    [JsonPropertyName("telecast_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TelecastId { get; init; }

    [JsonPropertyName("thumbnail_media_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ThumbnailMediaId { get; init; }

    [JsonPropertyName("available_for_replay")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AvailableForReplay { get; init; }

    [JsonPropertyName("is_locked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsLocked { get; init; }

    [JsonPropertyName("manual_publish")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ManualPublish { get; init; }

    [JsonPropertyName("recurrence")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScheduledBroadcastRecurrenceBody? Recurrence { get; init; }
}

internal sealed class ScheduledBroadcastRecurrenceBody
{
    [JsonPropertyName("frequency")]
    public required string Frequency { get; init; }

    [JsonPropertyName("repeats")]
    public required string Repeats { get; init; }
}

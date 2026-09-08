using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>PUT /2/broadcasts/scheduled/{id}</c>. <see cref="Id"/> is the path
/// segment (the alphanumeric UBS broadcast id); <see cref="ScheduledBroadcastId"/> is a
/// different, numeric scheduler id the registry requires in the body - the two id spaces don't
/// collapse into one (PAGE-02's "don't assume schemas coincide" applies to id spaces too).</summary>
public sealed class UpdateScheduledBroadcastRequest
{
    public required string Id { get; init; }

    public required string ScheduledBroadcastId { get; init; }

    public required DateTimeOffset ScheduledStart { get; init; }

    public required DateTimeOffset ScheduledEnd { get; init; }

    public string? Title { get; init; }

    public string? Locale { get; init; }

    public string? ChatOption { get; init; }

    public string? SourceId { get; init; }

    public string? ThumbnailMediaId { get; init; }

    public bool? AvailableForReplay { get; init; }

    public bool? IsLocked { get; init; }

    public bool? ManualPublish { get; init; }

    public bool? RollForward { get; init; }
}

/// <summary>Modeled from the "UpdateScheduledBroadcastResponse" schema.</summary>
public sealed class UpdateScheduledBroadcastResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ScheduledBroadcast? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class UpdateScheduledBroadcastBody
{
    [JsonPropertyName("scheduled_broadcast_id")]
    public required string ScheduledBroadcastId { get; init; }

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

    [JsonPropertyName("source_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceId { get; init; }

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

    [JsonPropertyName("roll_forward")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RollForward { get; init; }
}

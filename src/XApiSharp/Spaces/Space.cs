using System.Text.Json.Serialization;

namespace XApiSharp.Spaces;

/// <summary>Modeled from the "Space" schema.</summary>
public sealed class Space
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("lang")]
    public string? Lang { get; init; }

    [JsonPropertyName("creator_id")]
    public string? CreatorId { get; init; }

    [JsonPropertyName("host_ids")]
    public IReadOnlyList<string>? HostIds { get; init; }

    [JsonPropertyName("speaker_ids")]
    public IReadOnlyList<string>? SpeakerIds { get; init; }

    [JsonPropertyName("invited_user_ids")]
    public IReadOnlyList<string>? InvitedUserIds { get; init; }

    [JsonPropertyName("topic_ids")]
    public IReadOnlyList<string>? TopicIds { get; init; }

    [JsonPropertyName("is_ticketed")]
    public bool? IsTicketed { get; init; }

    [JsonPropertyName("participant_count")]
    public int? ParticipantCount { get; init; }

    [JsonPropertyName("subscriber_count")]
    public int? SubscriberCount { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("scheduled_start")]
    public DateTimeOffset? ScheduledStart { get; init; }

    [JsonPropertyName("started_at")]
    public DateTimeOffset? StartedAt { get; init; }

    [JsonPropertyName("ended_at")]
    public DateTimeOffset? EndedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

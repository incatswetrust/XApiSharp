using System.Text.Json.Serialization;

namespace XApiSharp.Chat;

/// <summary>Modeled from the "ChatConversation" schema.</summary>
public sealed class ChatConversation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("group_name")]
    public string? GroupName { get; init; }

    [JsonPropertyName("group_avatar_url")]
    public string? GroupAvatarUrl { get; init; }

    [JsonPropertyName("is_muted")]
    public bool? IsMuted { get; init; }

    [JsonPropertyName("message_ttl_ms")]
    public long? MessageTtlMs { get; init; }

    [JsonPropertyName("screen_capture_blocking_enabled")]
    public bool? ScreenCaptureBlockingEnabled { get; init; }

    [JsonPropertyName("screen_capture_detection_enabled")]
    public bool? ScreenCaptureDetectionEnabled { get; init; }

    [JsonPropertyName("admin_ids")]
    public IReadOnlyList<string>? AdminIds { get; init; }

    [JsonPropertyName("member_ids")]
    public IReadOnlyList<string>? MemberIds { get; init; }

    [JsonPropertyName("participant_ids")]
    public IReadOnlyList<string>? ParticipantIds { get; init; }
}

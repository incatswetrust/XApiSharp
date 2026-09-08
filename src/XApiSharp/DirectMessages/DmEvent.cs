using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.DirectMessages;

/// <summary>
/// Modeled from the "DmEvent" schema - ordinary Direct Messages only, not X Chat (spec section
/// 3.2's separate boundary; Chat's own event/message shapes land with that family in E5).
/// <c>event_type</c> stays a plain string (SER-06 - matches the same tradeoff as
/// <c>Post.ReplySettings</c>) rather than a closed enum, since it's a response value the server
/// could extend. <c>attachments</c>/<c>entities</c>/<c>referenced_posts</c> stay
/// <see cref="JsonElement"/> (SER-09).
/// </summary>
public sealed class DmEvent
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("event_type")]
    public string? EventType { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("dm_conversation_id")]
    public string? DmConversationId { get; init; }

    [JsonPropertyName("sender_id")]
    public string? SenderId { get; init; }

    [JsonPropertyName("participant_ids")]
    public IReadOnlyList<string>? ParticipantIds { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("attachments")]
    public JsonElement? Attachments { get; init; }

    [JsonPropertyName("entities")]
    public JsonElement? Entities { get; init; }

    [JsonPropertyName("referenced_posts")]
    public JsonElement? ReferencedPosts { get; init; }
}

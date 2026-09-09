using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Chat;

/// <summary>
/// Modeled from the "ChatMessageEvent" schema. Spec section 3.2: <see cref="EncodedEvent"/> is the
/// caller-decrypted event's opaque wire encoding - the SDK carries it through unmodified and never
/// attempts to decrypt or interpret it. <see cref="MessageEventSignature"/> is an untyped object
/// per the registry (SER-09/SER-12 escape hatch).
/// </summary>
public sealed class ChatMessageEvent
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; init; }

    [JsonPropertyName("conversation_token")]
    public string? ConversationToken { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("sender_id")]
    public string? SenderId { get; init; }

    [JsonPropertyName("previous_id")]
    public string? PreviousId { get; init; }

    [JsonPropertyName("encoded_event")]
    public string? EncodedEvent { get; init; }

    [JsonPropertyName("is_trusted")]
    public bool? IsTrusted { get; init; }

    [JsonPropertyName("message_event_signature")]
    public JsonElement? MessageEventSignature { get; init; }
}

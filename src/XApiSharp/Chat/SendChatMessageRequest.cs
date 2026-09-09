using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/{id}/messages</c>. Spec section 3.2:
/// <see cref="EncodedMessageCreateEvent"/> is the caller's own end-to-end-encrypted, already-encoded
/// message payload - the SDK transports it opaquely and never constructs or interprets it.</summary>
public sealed class SendChatMessageRequest
{
    public required string ConversationId { get; init; }

    /// <summary>Client-generated ID for this message.</summary>
    public required string MessageId { get; init; }

    public required string EncodedMessageCreateEvent { get; init; }

    public string? ConversationToken { get; init; }

    public string? EncodedMessageEventSignature { get; init; }
}

/// <summary>Modeled from the "SendChatMessageResponse" schema.</summary>
public sealed class SendChatMessageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public SendChatMessageResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class SendChatMessageResponseData
{
    /// <summary>Base64-encoded Thrift message event for the sent message, per the registry -
    /// opaque, carried through unmodified.</summary>
    [JsonPropertyName("encoded_message_event")]
    public required string EncodedMessageEvent { get; init; }
}

internal sealed class SendChatMessageBody
{
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }

    [JsonPropertyName("encoded_message_create_event")]
    public required string EncodedMessageCreateEvent { get; init; }

    [JsonPropertyName("conversation_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConversationToken { get; init; }

    [JsonPropertyName("encoded_message_event_signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncodedMessageEventSignature { get; init; }
}

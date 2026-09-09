namespace XApiSharp.Common;

/// <summary>The <c>chat_message_event.fields</c> query parameter (SER-05).</summary>
public enum XChatMessageEventField
{
    ConversationId,
    ConversationToken,
    CreatedAt,
    EncodedEvent,
    Id,
    IsTrusted,
    MessageEventSignature,
    PreviousId,
    SenderId,
}

public static class XChatMessageEventFieldExtensions
{
    public static string ToApiValue(this XChatMessageEventField field) => field switch
    {
        XChatMessageEventField.ConversationId => "conversation_id",
        XChatMessageEventField.ConversationToken => "conversation_token",
        XChatMessageEventField.CreatedAt => "created_at",
        XChatMessageEventField.EncodedEvent => "encoded_event",
        XChatMessageEventField.Id => "id",
        XChatMessageEventField.IsTrusted => "is_trusted",
        XChatMessageEventField.MessageEventSignature => "message_event_signature",
        XChatMessageEventField.PreviousId => "previous_id",
        XChatMessageEventField.SenderId => "sender_id",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

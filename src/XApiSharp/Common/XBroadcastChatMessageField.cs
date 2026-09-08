namespace XApiSharp.Common;

/// <summary>The <c>broadcast_chat_message.fields</c> query parameter (SER-05).</summary>
public enum XBroadcastChatMessageField
{
    AuthorName,
    AuthorUsername,
    BroadcastId,
    CreatedAtMs,
    Id,
    ReplyTo,
    Text,
}

public static class XBroadcastChatMessageFieldExtensions
{
    public static string ToApiValue(this XBroadcastChatMessageField field) => field switch
    {
        XBroadcastChatMessageField.AuthorName => "author_name",
        XBroadcastChatMessageField.AuthorUsername => "author_username",
        XBroadcastChatMessageField.BroadcastId => "broadcast_id",
        XBroadcastChatMessageField.CreatedAtMs => "created_at_ms",
        XBroadcastChatMessageField.Id => "id",
        XBroadcastChatMessageField.ReplyTo => "reply_to",
        XBroadcastChatMessageField.Text => "text",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

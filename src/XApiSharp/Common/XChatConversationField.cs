namespace XApiSharp.Common;

/// <summary>The <c>chat_conversation.fields</c> query parameter (SER-05).</summary>
public enum XChatConversationField
{
    CreatedAt,
    GroupAvatarUrl,
    GroupName,
    Id,
    IsMuted,
    MessageTtlMs,
    ScreenCaptureBlockingEnabled,
    ScreenCaptureDetectionEnabled,
    Type,
    UpdatedAt,
}

public static class XChatConversationFieldExtensions
{
    public static string ToApiValue(this XChatConversationField field) => field switch
    {
        XChatConversationField.CreatedAt => "created_at",
        XChatConversationField.GroupAvatarUrl => "group_avatar_url",
        XChatConversationField.GroupName => "group_name",
        XChatConversationField.Id => "id",
        XChatConversationField.IsMuted => "is_muted",
        XChatConversationField.MessageTtlMs => "message_ttl_ms",
        XChatConversationField.ScreenCaptureBlockingEnabled => "screen_capture_blocking_enabled",
        XChatConversationField.ScreenCaptureDetectionEnabled => "screen_capture_detection_enabled",
        XChatConversationField.Type => "type",
        XChatConversationField.UpdatedAt => "updated_at",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

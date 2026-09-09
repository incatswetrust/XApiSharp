namespace XApiSharp.Common;

/// <summary>The <c>delete_message_action</c> field on <c>POST /2/chat/conversations/{id}/messages/delete</c>.</summary>
public enum XChatDeleteMessageAction
{
    DeleteForAll,
    DeleteForSelf,
}

public static class XChatDeleteMessageActionExtensions
{
    public static string ToApiValue(this XChatDeleteMessageAction value) => value switch
    {
        XChatDeleteMessageAction.DeleteForAll => "delete_for_all",
        XChatDeleteMessageAction.DeleteForSelf => "delete_for_self",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

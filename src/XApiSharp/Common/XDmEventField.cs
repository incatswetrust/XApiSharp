namespace XApiSharp.Common;

/// <summary>The <c>dm_event.fields</c> query parameter (SER-05).</summary>
public enum XDmEventField
{
    Attachments,
    CreatedAt,
    DmConversationId,
    Entities,
    EventType,
    Id,
    Text,
}

public static class XDmEventFieldExtensions
{
    public static string ToApiValue(this XDmEventField field) => field switch
    {
        XDmEventField.Attachments => "attachments",
        XDmEventField.CreatedAt => "created_at",
        XDmEventField.DmConversationId => "dm_conversation_id",
        XDmEventField.Entities => "entities",
        XDmEventField.EventType => "event_type",
        XDmEventField.Id => "id",
        XDmEventField.Text => "text",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

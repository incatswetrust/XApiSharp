namespace XApiSharp.Common;

/// <summary>The <c>event_types</c> query parameter on the Direct Messages events-listing
/// operations - which kinds of events to include.</summary>
public enum XDmEventType
{
    MessageCreate,
    ParticipantsJoin,
    ParticipantsLeave,
}

public static class XDmEventTypeExtensions
{
    public static string ToApiValue(this XDmEventType value) => value switch
    {
        XDmEventType.MessageCreate => "MessageCreate",
        XDmEventType.ParticipantsJoin => "ParticipantsJoin",
        XDmEventType.ParticipantsLeave => "ParticipantsLeave",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

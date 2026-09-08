namespace XApiSharp.Common;

/// <summary>The <c>space.fields</c> query parameter (SER-05).</summary>
public enum XSpaceField
{
    CreatedAt,
    EndedAt,
    Id,
    IsTicketed,
    Lang,
    ParticipantCount,
    ScheduledStart,
    StartedAt,
    State,
    SubscriberCount,
    Title,
    UpdatedAt,
}

public static class XSpaceFieldExtensions
{
    public static string ToApiValue(this XSpaceField field) => field switch
    {
        XSpaceField.CreatedAt => "created_at",
        XSpaceField.EndedAt => "ended_at",
        XSpaceField.Id => "id",
        XSpaceField.IsTicketed => "is_ticketed",
        XSpaceField.Lang => "lang",
        XSpaceField.ParticipantCount => "participant_count",
        XSpaceField.ScheduledStart => "scheduled_start",
        XSpaceField.StartedAt => "started_at",
        XSpaceField.State => "state",
        XSpaceField.SubscriberCount => "subscriber_count",
        XSpaceField.Title => "title",
        XSpaceField.UpdatedAt => "updated_at",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

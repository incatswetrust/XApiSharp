namespace XApiSharp.Common;

/// <summary>The <c>poll.fields</c> query parameter (SER-05) - selects which optional
/// <see cref="Poll"/> fields to include in the response.</summary>
public enum XPollField
{
    DurationMinutes,
    EndDatetime,
    Id,
    Options,
    VotingStatus,
}

public static class XPollFieldExtensions
{
    public static string ToApiValue(this XPollField field) => field switch
    {
        XPollField.DurationMinutes => "duration_minutes",
        XPollField.EndDatetime => "end_datetime",
        XPollField.Id => "id",
        XPollField.Options => "options",
        XPollField.VotingStatus => "voting_status",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

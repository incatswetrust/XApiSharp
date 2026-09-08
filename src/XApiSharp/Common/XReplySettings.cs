namespace XApiSharp.Common;

/// <summary>The <c>reply_settings</c> value on <c>POST /2/tweets</c> - who can reply to a newly
/// created Post.</summary>
public enum XReplySettings
{
    Following,
    MentionedUsers,
    Subscribers,
    Verified,
}

public static class XReplySettingsExtensions
{
    /// <summary>Registry values are a mix of case conventions (<c>mentionedUsers</c> is camelCase,
    /// unlike almost every other string value in the API) - preserved exactly rather than
    /// normalized to snake_case.</summary>
    public static string ToApiValue(this XReplySettings value) => value switch
    {
        XReplySettings.Following => "following",
        XReplySettings.MentionedUsers => "mentionedUsers",
        XReplySettings.Subscribers => "subscribers",
        XReplySettings.Verified => "verified",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

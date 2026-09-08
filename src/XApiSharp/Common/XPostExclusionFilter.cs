namespace XApiSharp.Common;

/// <summary>The <c>exclude</c> query parameter on the Posts-family timeline-shaped list
/// operations (e.g. <c>GET /2/users/{id}/tweets</c>).</summary>
public enum XPostExclusionFilter
{
    Replies,
    Retweets,
}

public static class XPostExclusionFilterExtensions
{
    public static string ToApiValue(this XPostExclusionFilter filter) => filter switch
    {
        XPostExclusionFilter.Replies => "replies",
        XPostExclusionFilter.Retweets => "retweets",
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, message: null),
    };
}

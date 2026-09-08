namespace XApiSharp.Common;

/// <summary>The <c>trend.fields</c> query parameter (SER-05).</summary>
public enum XTrendField
{
    TrendName,
    TweetCount,
}

public static class XTrendFieldExtensions
{
    public static string ToApiValue(this XTrendField field) => field switch
    {
        XTrendField.TrendName => "trend_name",
        XTrendField.TweetCount => "tweet_count",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

namespace XApiSharp.Common;

/// <summary>The <c>granularity</c> query parameter on <c>GET /2/tweets/analytics</c> - a
/// different value set than <see cref="XCountGranularity"/>.</summary>
public enum XAnalyticsGranularity
{
    Hourly,
    Weekly,
    Daily,
    Total,
}

public static class XAnalyticsGranularityExtensions
{
    public static string ToApiValue(this XAnalyticsGranularity value) => value switch
    {
        XAnalyticsGranularity.Hourly => "hourly",
        XAnalyticsGranularity.Weekly => "weekly",
        XAnalyticsGranularity.Daily => "daily",
        XAnalyticsGranularity.Total => "total",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

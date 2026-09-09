namespace XApiSharp.Common;

/// <summary>The <c>granularity</c> query parameter on <c>GET /2/media/analytics</c> - yet another
/// distinct value set from <see cref="XAnalyticsGranularity"/> and <see cref="XCountGranularity"/>
/// (PAGE-02: the same parameter name means different things on different operations).</summary>
public enum XMediaAnalyticsGranularity
{
    Hourly,
    Daily,
    Total,
}

public static class XMediaAnalyticsGranularityExtensions
{
    public static string ToApiValue(this XMediaAnalyticsGranularity value) => value switch
    {
        XMediaAnalyticsGranularity.Hourly => "hourly",
        XMediaAnalyticsGranularity.Daily => "daily",
        XMediaAnalyticsGranularity.Total => "total",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

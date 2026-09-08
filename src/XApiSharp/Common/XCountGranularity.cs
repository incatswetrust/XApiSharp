namespace XApiSharp.Common;

/// <summary>The <c>granularity</c> query parameter on <c>GET /2/tweets/counts/*</c> - not the
/// same value set as <see cref="XAnalyticsGranularity"/> (PAGE-02's "don't assume all schemas are
/// the same" applies here too).</summary>
public enum XCountGranularity
{
    Minute,
    Hour,
    Day,
}

public static class XCountGranularityExtensions
{
    public static string ToApiValue(this XCountGranularity value) => value switch
    {
        XCountGranularity.Minute => "minute",
        XCountGranularity.Hour => "hour",
        XCountGranularity.Day => "day",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

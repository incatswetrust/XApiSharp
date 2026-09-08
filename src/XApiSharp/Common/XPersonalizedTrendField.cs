namespace XApiSharp.Common;

/// <summary>The <c>personalized_trend.fields</c> query parameter (SER-05).</summary>
public enum XPersonalizedTrendField
{
    Category,
    PostCount,
    TrendName,
    TrendingSince,
}

public static class XPersonalizedTrendFieldExtensions
{
    public static string ToApiValue(this XPersonalizedTrendField field) => field switch
    {
        XPersonalizedTrendField.Category => "category",
        XPersonalizedTrendField.PostCount => "post_count",
        XPersonalizedTrendField.TrendName => "trend_name",
        XPersonalizedTrendField.TrendingSince => "trending_since",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

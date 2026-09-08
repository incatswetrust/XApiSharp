namespace XApiSharp.Common;

/// <summary>The <c>sort_order</c> query parameter on the Posts search operations.</summary>
public enum XSortOrder
{
    Recency,
    Relevancy,
}

public static class XSortOrderExtensions
{
    public static string ToApiValue(this XSortOrder value) => value switch
    {
        XSortOrder.Recency => "recency",
        XSortOrder.Relevancy => "relevancy",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

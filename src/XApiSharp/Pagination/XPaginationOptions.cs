namespace XApiSharp.Pagination;

/// <summary>
/// Caller-tunable knobs for a paginated call (spec section 14, PAGE-04/PAGE-09). Passing
/// <see langword="null"/> to a paginated method is equivalent to <c>new XPaginationOptions()</c> -
/// no page/item cap, cycle detection still active, and partial errors still abort by default.
/// </summary>
public sealed class XPaginationOptions
{
    /// <summary>Stop after fetching this many pages. Applies to both the page-level and the
    /// item-level enumerable. <see langword="null"/> (default) means no cap - the sequence ends
    /// only when the endpoint stops returning a continuation token.</summary>
    public int? MaxPages { get; init; }

    /// <summary>Stop after yielding this many items. Only meaningful for the item-level
    /// <c>IAsyncEnumerable&lt;TItem&gt;</c> convenience methods - the page-level
    /// <c>IAsyncEnumerable&lt;XResponse&lt;TPage&gt;&gt;</c> methods ignore it, since a raw page
    /// has no SDK-defined notion of "item".</summary>
    public int? MaxItems { get; init; }

    /// <summary>PAGE-09: what an item-level enumerable does when it reaches a page that reports
    /// <c>HasErrors</c>. Defaults to <see cref="XPartialErrorPolicy.Throw"/> -
    /// silently dropping errored pages is opt-in only.</summary>
    public XPartialErrorPolicy PartialErrorPolicy { get; init; } = XPartialErrorPolicy.Throw;
}

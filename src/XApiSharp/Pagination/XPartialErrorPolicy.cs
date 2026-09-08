namespace XApiSharp.Pagination;

/// <summary>PAGE-09: how an item-level pagination enumerable reacts to a page carrying partial
/// errors alongside data.</summary>
public enum XPartialErrorPolicy
{
    /// <summary>Stop enumeration and throw <see cref="Errors.XPaginationPartialErrorException"/>
    /// when a page reports errors. The default - a caller has to opt into silently dropping
    /// errored pages, not the other way around.</summary>
    Throw = 0,

    /// <summary>Keep enumerating past a page that reports errors, yielding whatever items that
    /// page did return successfully.</summary>
    Ignore = 1,
}

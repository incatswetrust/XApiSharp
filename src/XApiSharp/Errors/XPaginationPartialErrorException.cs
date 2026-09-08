namespace XApiSharp.Errors;

/// <summary>PAGE-09: thrown from an item-level pagination enumerable when it reaches a page that
/// reports errors and <c>XPaginationOptions.PartialErrorPolicy</c> is left at the default
/// <c>Throw</c>. <see cref="Page"/> is the full page body that carried the errors, so the caller
/// doesn't lose access to it just because enumeration stopped.</summary>
public sealed class XPaginationPartialErrorException : XApiException
{
    /// <summary>The page body that reported errors, boxed as <see cref="object"/> since the
    /// exception type itself isn't generic over the endpoint's page type. Cast to the page type
    /// the failing call was declared with.</summary>
    public object? Page { get; }

    public XPaginationPartialErrorException(string message, object? page)
        : base(message)
    {
        Page = page;
    }
}

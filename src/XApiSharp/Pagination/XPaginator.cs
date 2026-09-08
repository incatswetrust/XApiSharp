using System.Runtime.CompilerServices;
using XApiSharp.Errors;

namespace XApiSharp.Pagination;

/// <summary>
/// The one shared engine every pageable family method builds on (spec section 14) - PAGE-01
/// through PAGE-10 are implemented once here, not re-derived per endpoint. A family's paged
/// method supplies three endpoint-specific pieces via delegates: how to fetch one page given a
/// continuation token (PAGE-02/PAGE-03 - the closure carries the fixed filters/fields/expansions
/// from the original request, only the token varies between calls), how to read the next token
/// out of a page body, and - for the item-level convenience overload only - how to read the item
/// list out of a page body.
/// </summary>
internal static class XPaginator
{
    /// <summary>
    /// PAGE-01: nothing happens until the caller starts enumerating - this is an iterator method,
    /// so no page is fetched until the first <c>MoveNextAsync</c>. PAGE-04: stops after
    /// <see cref="XPaginationOptions.MaxPages"/> pages (ignoring <c>MaxItems</c>, which only
    /// applies to <see cref="EnumerateItemsAsync{TPage,TItem}"/>) or when <paramref name="cancellationToken"/>
    /// fires. PAGE-05: an empty page with a valid next token keeps going - the loop only stops on
    /// a missing/empty token, never on item count. PAGE-06: throws <see cref="XTokenCycleException"/>
    /// the moment a token repeats, instead of looping forever. PAGE-07: early <c>break</c> out of
    /// the caller's <c>await foreach</c> disposes this iterator like any other - there is no
    /// unmanaged resource held between yields to clean up. PAGE-10: the token is only ever
    /// compared for equality, never parsed.
    /// </summary>
    /// <param name="fetchPage">Fetches one page for a given continuation token
    /// (<see langword="null"/> for the first page).</param>
    /// <param name="getNextToken">Reads the continuation token out of a page body, or
    /// <see langword="null"/>/empty when there is no next page. Endpoint-specific (PAGE-02) -
    /// callers read whichever field their operation's contract actually uses.</param>
    /// <param name="options">Caps and cancellation-adjacent knobs; <see langword="null"/> behaves
    /// like <c>new XPaginationOptions()</c>.</param>
    /// <param name="cancellationToken">Observed before each page fetch.</param>
    public static async IAsyncEnumerable<XResponse<TPage>> EnumeratePagesAsync<TPage>(
        Func<string?, CancellationToken, Task<XResponse<TPage>>> fetchPage,
        Func<TPage?, string?> getNextToken,
        XPaginationOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fetchPage);
        ArgumentNullException.ThrowIfNull(getNextToken);

        var maxPages = options?.MaxPages;
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);
        string? token = null;
        var pageCount = 0;

        while (maxPages is null || pageCount < maxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var response = await fetchPage(token, cancellationToken).ConfigureAwait(false);
            pageCount++;
            yield return response;

            var nextToken = getNextToken(response.Body);
            if (string.IsNullOrEmpty(nextToken))
            {
                yield break;
            }

            if (!seenTokens.Add(nextToken))
            {
                throw new XTokenCycleException(
                    $"Pagination token '{nextToken}' was returned twice - the endpoint appears to be cycling instead of terminating.",
                    nextToken);
            }

            token = nextToken;
        }
    }

    /// <summary>
    /// Item-level convenience built on <see cref="EnumeratePagesAsync{TPage}"/>: flattens each
    /// page into its items and additionally honors <see cref="XPaginationOptions.MaxItems"/>
    /// (PAGE-04) and <see cref="XPaginationOptions.PartialErrorPolicy"/> (PAGE-09).
    /// </summary>
    /// <param name="fetchPage">Fetches one page for a given continuation token
    /// (<see langword="null"/> for the first page).</param>
    /// <param name="getNextToken">Reads the continuation token out of a page body.</param>
    /// <param name="getItems">Reads the item list out of a non-null page body.</param>
    /// <param name="options">Caps and the partial-error policy; <see langword="null"/> behaves
    /// like <c>new XPaginationOptions()</c>.</param>
    /// <param name="cancellationToken">Observed before each page fetch.</param>
    public static async IAsyncEnumerable<TItem> EnumerateItemsAsync<TPage, TItem>(
        Func<string?, CancellationToken, Task<XResponse<TPage>>> fetchPage,
        Func<TPage?, string?> getNextToken,
        Func<TPage, IReadOnlyList<TItem>> getItems,
        XPaginationOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(getItems);

        var maxItems = options?.MaxItems;
        var policy = options?.PartialErrorPolicy ?? XPartialErrorPolicy.Throw;
        var itemCount = 0;

        await foreach (var response in EnumeratePagesAsync(fetchPage, getNextToken, options, cancellationToken).ConfigureAwait(false))
        {
            if (response.HasErrors && policy == XPartialErrorPolicy.Throw)
            {
                throw new XPaginationPartialErrorException(
                    "A page in the pagination sequence carried partial errors (XResponse.HasErrors). " +
                    "Inspect Page for details, or pass XPaginationOptions.PartialErrorPolicy = Ignore to keep enumerating past errored pages.",
                    response.Body);
            }

            if (response.Body is null)
            {
                continue;
            }

            foreach (var item in getItems(response.Body))
            {
                itemCount++;
                yield return item;

                // Checked *after* yielding, not before the next item: catching this only at the
                // top of the next item's processing would mean the cap already fell exactly on a
                // page boundary once - the outer `await foreach` above would have advanced to
                // fetch one more page than the caller actually needed before this loop ever got a
                // chance to say "stop". Checking right here means the very last page fetched is
                // always the one that contains the MaxItems'th item, never the one after it.
                if (maxItems is { } max && itemCount >= max)
                {
                    yield break;
                }
            }
        }
    }
}

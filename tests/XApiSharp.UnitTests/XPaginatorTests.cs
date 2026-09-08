using System.Net;
using XApiSharp.Errors;
using XApiSharp.Pagination;

namespace XApiSharp.UnitTests;

/// <summary>
/// Unit coverage for the shared pagination engine (spec section 14, PAGE-01..10). No HTTP is
/// involved here - fake pages are handed back directly, since <see cref="XPaginator"/> only cares
/// about the fetch/next-token/get-items delegate contract, not how a page is actually retrieved.
/// </summary>
public class XPaginatorTests
{
    private sealed record FakePage(IReadOnlyList<string> Items, string? NextToken);

    [Fact]
    public void Does_not_fetch_any_page_before_enumeration_begins()
    {
        // PAGE-01.
        var callCount = 0;
        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (_, _) => { callCount++; return Task.FromResult(Page(["a"], null)); },
            p => p?.NextToken,
            options: null,
            cancellationToken: default);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public async Task Fetches_only_on_first_MoveNextAsync()
    {
        var callCount = 0;
        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (_, _) => { callCount++; return Task.FromResult(Page(["a"], null)); },
            p => p?.NextToken,
            options: null,
            cancellationToken: default);

        await using var enumerator = enumerable.GetAsyncEnumerator();
        Assert.Equal(0, callCount);

        await enumerator.MoveNextAsync();
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Forwards_the_endpoint_specific_next_token_to_the_following_fetch()
    {
        // PAGE-02/PAGE-03: the token from page N is exactly what page N+1's fetch receives.
        var receivedTokens = new List<string?>();

        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (token, _) =>
            {
                receivedTokens.Add(token);
                var page = token is null ? new FakePage(["a"], "tok-1") : new FakePage(["b"], null);
                return Task.FromResult(Wrap(page));
            },
            p => p?.NextToken,
            options: null,
            cancellationToken: default);

        var pageBodies = new List<FakePage?>();
        await foreach (var response in enumerable)
        {
            pageBodies.Add(response.Body);
        }

        Assert.Equal([null, "tok-1"], receivedTokens);
        Assert.Equal(2, pageBodies.Count);
    }

    [Fact]
    public async Task MaxPages_stops_the_page_level_sequence_without_fetching_further_pages()
    {
        // PAGE-04.
        var callCount = 0;
        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (_, _) => { callCount++; return Task.FromResult(Page(["a"], $"tok-{callCount}")); },
            p => p?.NextToken,
            new XPaginationOptions { MaxPages = 3 },
            cancellationToken: default);

        var pages = new List<FakePage?>();
        await foreach (var response in enumerable)
        {
            pages.Add(response.Body);
        }

        Assert.Equal(3, pages.Count);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task MaxItems_stops_the_item_level_sequence_mid_page_without_fetching_the_next_page()
    {
        // PAGE-04, item-level: an infinite endpoint-side sequence, but we only ask for 3 items
        // out of pages of 2 - the 3rd item comes from page 2, so page 3 must never be fetched.
        var callCount = 0;
        Task<XResponse<FakePage>> Fetch(string? token, CancellationToken _)
        {
            callCount++;
            var n = callCount;
            return Task.FromResult(Wrap(new FakePage([$"{n}a", $"{n}b"], $"tok-{n}")));
        }

        var items = new List<string>();
        await foreach (var item in XPaginator.EnumerateItemsAsync<FakePage, string>(
            Fetch,
            p => p?.NextToken,
            p => p.Items,
            new XPaginationOptions { MaxItems = 3 },
            cancellationToken: default))
        {
            items.Add(item);
        }

        Assert.Equal(["1a", "1b", "2a"], items);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task MaxItems_stops_exactly_on_a_page_boundary_without_fetching_one_page_too_many()
    {
        // Same guarantee as the test above, but with exactly one item per page - the case where
        // the MaxItems'th item is also the *last* item on its page. Checking the cap before
        // processing the next item (instead of right after yielding) would have let the outer
        // page loop already fetch one extra page by the time that check ever ran.
        var callCount = 0;
        Task<XResponse<FakePage>> Fetch(string? _, CancellationToken __)
        {
            callCount++;
            var n = callCount;
            return Task.FromResult(Wrap(new FakePage([$"{n}a"], $"tok-{n}")));
        }

        var items = new List<string>();
        await foreach (var item in XPaginator.EnumerateItemsAsync<FakePage, string>(
            Fetch, p => p?.NextToken, p => p.Items,
            new XPaginationOptions { MaxItems = 2 }, cancellationToken: default))
        {
            items.Add(item);
        }

        Assert.Equal(["1a", "2a"], items);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Empty_intermediate_page_with_a_valid_next_token_does_not_end_the_sequence()
    {
        // PAGE-05.
        var callCount = 0;
        Task<XResponse<FakePage>> Fetch(string? token, CancellationToken _)
        {
            callCount++;
            return callCount == 1
                ? Task.FromResult(Wrap(new FakePage([], "tok-2")))
                : Task.FromResult(Wrap(new FakePage(["only-item"], null)));
        }

        var items = new List<string>();
        await foreach (var item in XPaginator.EnumerateItemsAsync<FakePage, string>(
            Fetch, p => p?.NextToken, p => p.Items, options: null, cancellationToken: default))
        {
            items.Add(item);
        }

        Assert.Equal(2, callCount);
        Assert.Equal(["only-item"], items);
    }

    [Fact]
    public async Task A_repeated_token_throws_a_token_cycle_exception_instead_of_looping_forever()
    {
        // PAGE-06.
        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (_, _) => Task.FromResult(Page(["a"], "same-token")),
            p => p?.NextToken,
            options: null,
            cancellationToken: default);

        var ex = await Assert.ThrowsAsync<XTokenCycleException>(async () =>
        {
            await foreach (var _ in enumerable)
            {
                // Drain until the cycle is detected.
            }
        });

        Assert.Equal("same-token", ex.Token);
    }

    [Fact]
    public async Task Breaking_out_of_the_foreach_early_stops_fetching_further_pages()
    {
        // PAGE-07.
        var callCount = 0;
        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (_, _) => { callCount++; return Task.FromResult(Page(["a"], $"tok-{callCount}")); },
            p => p?.NextToken,
            options: null,
            cancellationToken: default);

        await foreach (var _ in enumerable)
        {
            break;
        }

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task Cancellation_stops_enumeration_before_the_next_fetch()
    {
        // PAGE-04 (cancellation).
        using var cts = new CancellationTokenSource();
        var callCount = 0;
        var enumerable = XPaginator.EnumeratePagesAsync<FakePage>(
            (_, _) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    cts.Cancel();
                }

                return Task.FromResult(Page(["a"], $"tok-{callCount}"));
            },
            p => p?.NextToken,
            options: null,
            cancellationToken: cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in enumerable.WithCancellation(cts.Token))
            {
                // First page comes through, then the cancellation requested inside the first
                // fetch must stop the second one from starting.
            }
        });

        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task A_page_with_errors_throws_by_default_and_exposes_the_page()
    {
        // PAGE-09 default (Throw).
        var page = new FakePage(["a"], null);
        Task<XResponse<FakePage>> Fetch(string? _, CancellationToken __) =>
            Task.FromResult(Wrap(page, hasErrors: true));

        var ex = await Assert.ThrowsAsync<XPaginationPartialErrorException>(async () =>
        {
            await foreach (var _ in XPaginator.EnumerateItemsAsync<FakePage, string>(
                Fetch, p => p?.NextToken, p => p.Items, options: null, cancellationToken: default))
            {
                // Never reached.
            }
        });

        Assert.Same(page, ex.Page);
    }

    [Fact]
    public async Task A_page_with_errors_is_kept_when_the_policy_is_set_to_ignore()
    {
        // PAGE-09 explicit opt-in (Ignore).
        Task<XResponse<FakePage>> Fetch(string? _, CancellationToken __) =>
            Task.FromResult(Wrap(new FakePage(["a"], null), hasErrors: true));

        var items = new List<string>();
        await foreach (var item in XPaginator.EnumerateItemsAsync<FakePage, string>(
            Fetch, p => p?.NextToken, p => p.Items,
            new XPaginationOptions { PartialErrorPolicy = XPartialErrorPolicy.Ignore },
            cancellationToken: default))
        {
            items.Add(item);
        }

        Assert.Equal(["a"], items);
    }

    private static XResponse<FakePage> Page(IReadOnlyList<string> items, string? nextToken = null) =>
        Wrap(new FakePage(items, nextToken));

    private static XResponse<FakePage> Wrap(FakePage page, bool hasErrors = false) => new()
    {
        Body = page,
        StatusCode = HttpStatusCode.OK,
        Headers = new Dictionary<string, IReadOnlyList<string>>(),
        HasErrors = hasErrors,
        IsPartialSuccess = hasErrors,
    };
}

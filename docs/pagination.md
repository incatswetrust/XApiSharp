# Pagination

Every paged X API list operation (followers, search results, a user's posts, ...) is exposed as
two methods on the family client, both built on the same shared engine (`XPaginator`, spec section
14, PAGE-01..10) - no per-endpoint pagination logic to get wrong:

- `<Thing>PagesAsync(...)` - `IAsyncEnumerable<XResponse<TPage>>`, one element per raw page. Use
  this when you need page-level metadata (`Body.Meta`) or want to see `XResponse.HasErrors` before
  deciding whether to trust a page's items.
- `<Thing>Async(...)` - `IAsyncEnumerable<TItem>`, flattened to individual items across all pages.
  Use this for the common case of "just give me the items."

```csharp
// Item-level: iterate followers directly, no manual token juggling.
await foreach (var user in client.Users.GetFollowersAsync(new GetUsersPageRequest { UserId = userId }))
{
    Console.WriteLine(user.Username);
}

// Page-level: inspect each page's Meta/HasErrors before trusting its items.
await foreach (var page in client.Users.GetFollowersPagesAsync(new GetUsersPageRequest { UserId = userId }))
{
    Console.WriteLine($"page had {page.Body?.Data?.Count ?? 0} users, ResultCount={page.Body?.Meta?.ResultCount}");
}
```

## How it works

- **Lazy (PAGE-01).** Nothing is fetched until you start enumerating - constructing the
  `IAsyncEnumerable` does no I/O.
- **Token-driven, not count-driven (PAGE-05).** The sequence continues as long as the endpoint
  returns a next-page token, even across a page with zero items. It only stops when a page's token
  is missing/empty, or a cap below is hit.
- **Tokens are opaque (PAGE-10).** They're compared for equality only, never parsed or constructed
  by the SDK - treat them the same way in your own code if you ever persist one.
- **Cycle detection (PAGE-06).** If the same token is ever returned twice, enumeration throws
  `XTokenCycleException` instead of looping forever.
- **`await foreach` `break` is enough (PAGE-07).** Stopping early disposes the iterator cleanly;
  there's no separate "stop paginating" call to remember.

## Capping and partial errors

Pass `XPaginationOptions` to either method form:

```csharp
var options = new XPaginationOptions
{
    MaxItems = 500,                                  // item-level only; page-level ignores it
    MaxPages = 50,                                    // both forms honor this
    PartialErrorPolicy = XPartialErrorPolicy.Ignore,   // default: Throw
};

await foreach (var user in client.Users.GetFollowersAsync(request, options))
{
    // ...
}
```

- `MaxItems` stops the item-level enumerable exactly on the item that reaches the cap - it never
  fetches one page more than needed to reach it. It has no effect on the page-level enumerable
  (a raw page has no SDK-defined notion of "item").
- `MaxPages` applies to both forms.
- `PartialErrorPolicy` (item-level only, PAGE-09) controls what happens when a page comes back with
  `XResponse.HasErrors == true`: the default `Throw` raises `XPaginationPartialErrorException`
  immediately so you don't silently iterate past a page X's API reported problems with; `Ignore`
  keeps going and simply yields that page's items as-is. The page-level enumerable never throws for
  this - it hands you the `XResponse` (including `HasErrors`) and lets you decide.

## Cancellation

Both forms observe the `CancellationToken` you pass before each page fetch - cancelling mid-page
does not cancel a page already in flight, but stops the next fetch and lets the enumeration exit
via `OperationCanceledException` the same way any other cancelled async call does.

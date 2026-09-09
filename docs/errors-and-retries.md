# Errors, retries, and rate limits

## Exception hierarchy

Every non-success HTTP status raises `XApiException` (`XApiSharp.Errors`) or one of its derived
types - catch the base type if you just need "something went wrong," or a specific derived type to
handle a situation differently:

| Type | When |
| --- | --- |
| `XAuthenticationException` | 401 - bad/expired credentials |
| `XAccessDeniedException` | 403 - authenticated, but not permitted |
| `XRateLimitException` | 429 - see below |
| `XRequestTimeoutException` | the operation/attempt deadline (or an external `HttpClient.Timeout`) elapsed |
| `XTransportException` | no HTTP response at all - DNS, connection refused, TLS failure |
| `XProtocolException` | the server responded, but not in the shape the contract promises |
| `XStreamConnectionLostException` / `XStreamIdleTimeoutException` / `XStreamMessageTooLargeException` / `XStreamMalformedMessageException` | streaming-specific, see `docs/streaming.md` |
| `XMediaUploadException` | a chunked media upload failed partway through, see `docs/media.md` |
| `XPaginationPartialErrorException` / `XTokenCycleException` | pagination-specific, see `docs/pagination.md` |
| `XJobPollingException` | a compliance job's status poll gave up or the job itself failed |

`XApiException` carries `StatusCode`, `Problem` (the parsed RFC-7807-style problem body, when the
server sent one - `Type`/`Title`/`Detail`/`Status` plus variant-specific fields in
`Problem.ExtensionData`), and `RequestId` (for support/log correlation) - it never leaks a raw
response body by default.

```csharp
try
{
    await client.Posts.CreateAsync(request);
}
catch (XRateLimitException ex)
{
    Console.WriteLine($"rate limited, retry after {ex.RetryAfter}");
}
catch (XApiException ex)
{
    Console.WriteLine($"{ex.StatusCode} {ex.Problem?.Title}: {ex.Problem?.Detail} (request {ex.RequestId})");
}
```

## Partial success (HTTP 200 with an `errors` array)

Some list endpoints can return HTTP 200 with a body that's a mix of successful items and per-item
errors - the SDK never silently turns that into "fully successful." Check `XResponse.HasErrors`
(and `IsPartialSuccess`, true only when the body has *both* successes and errors) before trusting
that every item in a response came back clean:

```csharp
var response = await client.Users.GetByIdsAsync(request);
if (response.HasErrors)
{
    // response.Body still has whatever data did come back; inspect it alongside the errors.
}
```

Pagination's item-level enumerables apply the same idea via `XPaginationOptions.PartialErrorPolicy`
- see `docs/pagination.md`.

## Retries

Automatic retries are narrow and conservative by design:

- **GET/HEAD only.** Writes are never retried automatically, regardless of `MaxRetries` - a
  retried write could double-create/double-delete something, so that decision is always yours (see
  "Uncertain write outcomes" below).
- **Only on a transient network error, an allowed 5xx, or a documented temporary 429.**
- **`XClientOptions.MaxRetries`** (default 2) - retries after the original attempt.
- **`XClientOptions.MaxRetryDelay`** (default 15s) - caps a single computed backoff delay. A 429's
  own `Retry-After`/reset hint is still honored even past this cap, but `OperationTimeout` remains
  the real ceiling - a long rate-limit wait can still end in `XRequestTimeoutException` if it
  would blow the overall deadline.
- **`XClientOptions.OperationTimeout`** (default 30s) - the overall deadline for one logical call,
  spanning every retry attempt.
- **`XClientOptions.AttemptTimeout`** (default 30s) - each individual attempt (original or retry)
  gets its own fresh window within the operation deadline.

```csharp
var client = new XApiClient(httpClient, auth, new XClientOptions
{
    MaxRetries = 3,
    OperationTimeout = TimeSpan.FromSeconds(45),
});
```

The `HttpClient` you pass in has its own `HttpClient.Timeout` (100 seconds by default, not
infinite) - if it's shorter than `OperationTimeout`/`AttemptTimeout`, it wins silently at the BCL
level. The SDK detects this specific case and still surfaces a clear `XRequestTimeoutException`
rather than an ambiguous cancellation, but it can't make the external client wait longer than you
configured it for - raise `HttpClient.Timeout` (or set it to `Timeout.InfiniteTimeSpan`) if you
want `XClientOptions` to be the sole authority on timing.

## Rate limits

`XRateLimitException.RetryAfter` is populated only when the server provided a usable hint -
never guessed or defaulted when absent. If you're calling an endpoint often enough to hit limits
regularly, back off using that value rather than a fixed sleep.

## Uncertain write outcomes

A write call (create/update/delete) that fails with `XTransportException` or
`XRequestTimeoutException` leaves you not knowing whether the server actually received and applied
it before the connection dropped - the SDK does not retry writes automatically for exactly this
reason. Decide deliberately: check for the effect (e.g. look up the resource you tried to create)
before retrying the call yourself, rather than assuming a timeout means "nothing happened."

## Diagnostics

Every request emits one `System.Diagnostics.Activity` (source name `"XApiSharp"`) spanning all of
its retries, plus events for a retry, a rate-limit wait, a token refresh, a stream
connect/disconnect, and a response-deserialization failure. A matching `System.Diagnostics.Metrics.Meter`
(also named `"XApiSharp"`) reports request count/duration, retry count, error count, active
stream-connection count, and dropped (deduplicated) stream-event count. Tags are limited to the
HTTP method and a best-effort route template (e.g. `2/users/{id}`) - never a user ID, a real URL
with its query string, a token, or Post/message text.

Nothing is exported anywhere on its own - plug in an `ActivityListener`/`MeterListener` directly,
or add the OpenTelemetry SDK to your own app and point it at the `"XApiSharp"` source/meter names;
with nothing listening, the instrumentation calls are close to free.

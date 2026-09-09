# Streaming

Every long-lived streaming endpoint (filtered/sampled/volume Post streams, compliance streams,
Like streams, Activity streams) is exposed the same way: an `IAsyncEnumerable<TBody>` that pulls
one NDJSON line per `MoveNextAsync` - there's no internal event queue anywhere in the engine, so
backpressure is structural (STREAM-04): the connection only advances as fast as you consume it.

```csharp
await foreach (var evt in client.Streaming.StreamPostsAsync(new FilteredPostStreamRequest(), cancellationToken: cts.Token))
{
    Console.WriteLine(evt.Data?.Text);
}
```

Stop by cancelling the token or `break`-ing out of the loop - both dispose the underlying
connection cleanly (STREAM-12: stopping enumeration is a caller decision, never something the SDK
decides on its own based on event count or elapsed time).

## Managing filter rules (filtered stream only)

```csharp
await client.Streaming.UpdateRulesAsync(new UpdateStreamRulesRequest
{
    Add = [new StreamRuleToAdd { Value = "from:XDevelopers", Tag = "xdev" }],
});

await foreach (var rule in client.Streaming.GetRulesAsync(new GetStreamRulesRequest()))
{
    Console.WriteLine($"{rule.Id}: {rule.Value} ({rule.Tag})");
}
```

`GetRulesAsync`/`GetRulesPagesAsync` follow the same pagination shape as every other paged
operation - see `docs/pagination.md`.

## `XStreamOptions<TBody>`

Every `Stream*Async` method takes an optional `XStreamOptions<TBody>`:

- **`MaxMessageSizeBytes`** (STREAM-03, default 1 MiB) - caps a single NDJSON line's buffered size
  before a newline is found. Raise it for streams whose events run larger than the default (e.g.
  Posts with large `note_tweet` bodies).
- **`IdleTimeout`** (STREAM-08, default `null` = disabled) - throws `XStreamIdleTimeoutException`
  if no data (including heartbeat lines) arrives within the window. The SDK doesn't invent a
  default heartbeat cadence per stream; consult X's current docs for the stream you're calling and
  set this a comfortable margin above its documented cadence.
- **`Reconnect`** (STREAM-07, default `null` = disabled) - see below.
- **`DeduplicationKey`** (STREAM-10, default `null` = disabled) - see below.

```csharp
var options = new XStreamOptions<StreamPostResponse>
{
    IdleTimeout = TimeSpan.FromSeconds(30),
    Reconnect = new XStreamReconnectOptions
    {
        MaxAttempts = 5,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(30),
        OnReconnecting = new Progress<XStreamReconnectEvent>(e => Console.WriteLine($"reconnect #{e.AttemptNumber}: {e.Cause.Message}")),
    },
};

await foreach (var evt in client.Streaming.StreamPostsSampleAsync(new PostSampleStreamRequest(), options, cts.Token))
{
    // ...
}
```

### Reconnect

Off by default - a lost connection ends the enumeration by throwing
`XStreamConnectionLostException`, with no automatic retry. Opting in with `Reconnect` gets you a
**bounded** attempt budget (`MaxAttempts`, counted across the whole enumeration's lifetime, not
reset after a successful stretch) and exponential backoff between `InitialBackoff` and
`MaxBackoff` - never an unlimited silent retry loop. Only a lost connection or a retryable
connect-time failure (transport error, rate limit) triggers a reconnect; authentication errors,
oversized messages, and malformed JSON never do (STREAM-11) - those are always your bug or your
credentials, not a transient blip.

Reconnecting always means some events between the disconnect and the new connection are
unrecoverable - there's no exactly-once or replay-all guarantee (STREAM-09/STREAM-05). Set
`OnReconnecting` if you need to know it happened (e.g. to log a gap), rather than the SDK silently
papering over the loss.

### Deduplication

Off unless you supply `DeduplicationKey`. A bare post/event ID isn't always a safe dedup key by
itself (the same ID can recur for a different event type, or a later edited version), so the SDK
doesn't invent one - give it a key that incorporates whatever actually distinguishes a genuine
duplicate for the event type you're consuming:

```csharp
DeduplicationKey = evt => evt.Data?.Id ?? "",
```

Recently-seen keys are tracked in a bounded FIFO window (`DeduplicationWindowSize`, default
10,000) - never unbounded, for the same reason the engine has no internal event queue.

## Cancellation and disposal

Cancelling the token passed to a `Stream*Async` call, or breaking out of `await foreach`, closes
the underlying HTTP connection and stops enumeration cleanly - there is no separate "stop"/"close"
method to remember to call.

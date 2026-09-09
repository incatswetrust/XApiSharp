namespace XApiSharp.Streaming;

/// <summary>
/// Per-call tuning for a streaming enumeration (spec section 16). There is deliberately no
/// internal event queue/channel anywhere in this engine (see <see cref="XEventStream"/>) - the
/// enumerator only ever reads the next line when the consumer calls <c>MoveNextAsync</c>, so
/// backpressure (STREAM-04) is structural, not a buffer that needs a size limit or a drop policy.
/// </summary>
public sealed class XStreamOptions<TBody>
{
    /// <summary>STREAM-03: caps a single NDJSON line's buffered size before a newline is found.
    /// 1 MiB default - override per stream if you know its events run larger (e.g. Posts with
    /// large <c>note_tweet</c> bodies).</summary>
    public int MaxMessageSizeBytes { get; init; } = 1024 * 1024;

    /// <summary>STREAM-08: no data (including heartbeat lines) within this window throws
    /// <see cref="Errors.XStreamIdleTimeoutException"/>. <see langword="null"/> (default) disables
    /// idle detection - the SDK doesn't invent a default heartbeat interval per stream; consult
    /// the current X docs for the specific stream you're calling and set this a comfortable margin
    /// above its documented heartbeat cadence.</summary>
    public TimeSpan? IdleTimeout { get; init; }

    /// <summary>STREAM-07: <see langword="null"/> (default) means a lost connection ends the
    /// enumeration by throwing, with no automatic reconnect.</summary>
    public XStreamReconnectOptions? Reconnect { get; init; }

    /// <summary>
    /// STREAM-10: deduplication is off unless you supply a key selector - a bare post/event ID
    /// is not always a safe key by itself (the same ID can recur for a different event type or a
    /// later edited version), so the SDK doesn't invent one; give it a key that incorporates
    /// whatever distinguishes a genuine duplicate for the event type you're consuming. A throwing
    /// selector propagates rather than being swallowed.
    /// </summary>
    public Func<TBody, string>? DeduplicationKey { get; init; }

    /// <summary>Bounded FIFO window of recently-seen keys (oldest evicted first) when
    /// <see cref="DeduplicationKey"/> is set - never unbounded (STREAM-03's "internal queues" limit
    /// applies here too). Ignored when <see cref="DeduplicationKey"/> is null.</summary>
    public int DeduplicationWindowSize { get; init; } = 10_000;
}

namespace XApiSharp.Streaming;

/// <summary>
/// STREAM-07: reconnect is off by default (a null <see cref="XStreamOptions{TBody}.Reconnect"/>) -
/// enabling it is an explicit opt-in with a bounded attempt count and backoff, never an unlimited
/// silent retry loop. Only <see cref="Errors.XStreamConnectionLostException"/> and a retryable
/// connect-time failure (transport error, rate limit) are eligible; authentication errors,
/// oversized messages, and malformed JSON are never retried this way (STREAM-11).
/// </summary>
public sealed class XStreamReconnectOptions
{
    /// <summary>Total reconnect attempts across the whole enumeration's lifetime, not a
    /// per-failure-streak counter that resets after a successful stretch of streaming - a
    /// long-running consumer that reconnects occasionally over days will eventually exhaust this
    /// budget even if each individual reconnect succeeded. 0 disables reconnect even though this
    /// object is non-null (equivalent to not setting <see cref="XStreamOptions{TBody}.Reconnect"/>
    /// at all, but useful when the value is computed).</summary>
    public required int MaxAttempts { get; init; }

    public TimeSpan InitialBackoff { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan MaxBackoff { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>STREAM-05: reconnect always means some events between the disconnect and the new
    /// connection are unrecoverable (no exactly-once/replay-all guarantee - STREAM-09) - this
    /// callback reports that it happened rather than doing it silently. A throwing callback
    /// propagates, it is not swallowed.</summary>
    public IProgress<XStreamReconnectEvent>? OnReconnecting { get; init; }
}

/// <summary>One reconnect attempt: which attempt number, and the exception that triggered it.</summary>
public sealed record XStreamReconnectEvent(int AttemptNumber, Exception Cause);

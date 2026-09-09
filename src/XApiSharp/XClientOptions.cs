namespace XApiSharp;

/// <summary>Client-wide settings for transport timeouts and retry policy (spec sections 11, 13).</summary>
public sealed class XClientOptions
{
    /// <summary>
    /// The trusted API host (spec HTTP-09: fixed per official contract, overridable for tests).
    /// </summary>
    public Uri BaseUrl { get; init; } = new("https://api.x.com");

    /// <summary>
    /// Overall deadline for one logical operation, spanning every retry attempt (spec HTTP-07).
    /// This is SDK policy, not an X API limit - see spec section 13.2.
    /// </summary>
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Deadline for a single HTTP attempt (spec HTTP-07) - each retry gets its own fresh window.
    /// </summary>
    /// <remarks>
    /// HTTP-08: the external <see cref="HttpClient"/> passed to <see cref="XApiClient"/> has its
    /// own <see cref="HttpClient.Timeout"/> (100 seconds by default, not infinite). If that is
    /// shorter than <see cref="OperationTimeout"/>/<see cref="AttemptTimeout"/>, it wins silently
    /// at the BCL level - the SDK detects this specific case (the BCL wraps it in a
    /// <see cref="TimeoutException"/> inner exception) and surfaces a clear
    /// <c>XRequestTimeoutException</c> instead of an ambiguous cancellation, but it cannot make
    /// the external client wait longer than it was configured to. Raise
    /// <see cref="HttpClient.Timeout"/> above these values (or set it to
    /// <see cref="Timeout.InfiniteTimeSpan"/>) to let <see cref="XClientOptions"/> govern timing
    /// on its own.
    /// </remarks>
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Retries after the initial attempt, for GET/HEAD requests only on a transient network
    /// error, an allowed 5xx, or a documented temporary 429 (spec section 13.2). Writes are
    /// never retried automatically regardless of this setting. Default 2 - "no more than two
    /// retries after the original attempt".
    /// </summary>
    public int MaxRetries { get; init; } = 2;

    /// <summary>
    /// Cap on a single computed backoff delay (spec section 13.2: "a single delay caps out at
    /// 15 seconds"). A 429 response's own Retry-After/reset hint is honored even if it
    /// exceeds this cap - see <see cref="XClientOptions"/> remarks on <see cref="OperationTimeout"/>
    /// for why that can still fail: the overall deadline is the real ceiling.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// SER-11: hard ceiling on a buffered (non-streaming) response body, enforced while reading
    /// even if the server didn't send (or lied about) Content-Length. Default 10 MiB - generous
    /// for any current JSON response shape, small next to an accidental unbounded download.
    /// Streaming/media endpoints (E5) will have their own explicit buffer settings, not this one.
    /// </summary>
    public long MaxResponseBufferSize { get; init; } = 10 * 1024 * 1024;
}

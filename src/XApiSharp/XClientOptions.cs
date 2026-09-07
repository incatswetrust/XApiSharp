namespace XApiSharp;

/// <summary>
/// Client-wide settings. Retry/backoff policy (spec section 13.2) is added in a later E3 commit;
/// this covers the transport-level timeouts (spec HTTP-07).
/// </summary>
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
    /// Deadline for a single HTTP attempt (spec HTTP-07). Until retry lands, this has the same
    /// practical effect as <see cref="OperationTimeout"/> for a single-attempt call; it becomes
    /// meaningful once each retry gets its own fresh attempt window.
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
}

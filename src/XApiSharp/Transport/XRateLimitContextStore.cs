using System.Collections.Concurrent;

namespace XApiSharp.Transport;

/// <summary>
/// Per-auth-context rate-limit state, keyed by endpoint scope within that context (RATE-03: "store
/// state by the actual scope of the limit - endpoint/family, and app and/or user"). One instance
/// exists per <see cref="Authentication.IXAuthenticationProvider"/> instance (see
/// <see cref="XRateLimitStateRegistry"/>) - since this SDK already establishes "one provider
/// instance per user" as an invariant (docs/authentication.md, docs/di-and-multi-user.md;
/// app-only naturally shares one instance across all app-only calls), keying by the provider
/// instance is what makes RATE-04 ("exhausting one user's limit must not affect others") hold
/// without this type needing its own separate notion of "who is this".
/// </summary>
internal sealed class XRateLimitContextStore
{
    private readonly ConcurrentDictionary<string, Entry> _entriesByScope = new(StringComparer.Ordinal);
    private long _sequenceCounter;

    /// <summary>
    /// RATE-05: a monotonic ticket, not a timestamp - two responses in the same rate-limit window
    /// share the same <c>reset</c> value, so only the *order requests were sent in* can tell which
    /// one is more recent. Call this once per attempt, before sending, and pass the result to
    /// <see cref="Record"/> after the response comes back - whichever attempt started later always
    /// wins the write, regardless of which one's response arrives first.
    /// </summary>
    public long NextSequence() => Interlocked.Increment(ref _sequenceCounter);

    /// <summary>Records a response's rate-limit info for <paramref name="scopeKey"/>, unless a
    /// later-sequenced response already recorded something for the same scope (RATE-05).</summary>
    public void Record(string scopeKey, long sequence, XRateLimitInfo info)
    {
        _entriesByScope.AddOrUpdate(
            scopeKey,
            static (_, state) => new Entry(state.Sequence, state.Info),
            static (_, existing, state) => state.Sequence > existing.Sequence ? new Entry(state.Sequence, state.Info) : existing,
            (Sequence: sequence, Info: info));
    }

    /// <summary>The most recently-recorded rate-limit info for <paramref name="scopeKey"/>, or
    /// <see langword="null"/> if nothing has been recorded yet.</summary>
    public XRateLimitInfo? TryGet(string scopeKey) =>
        _entriesByScope.TryGetValue(scopeKey, out var entry) ? entry.Info : null;

    private readonly record struct Entry(long Sequence, XRateLimitInfo Info);
}

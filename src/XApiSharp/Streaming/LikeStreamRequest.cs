using XApiSharp.Common;

namespace XApiSharp.Streaming;

/// <summary>Request shared by <c>streamLikesFirehose</c> (partition 1-20) and
/// <c>streamLikesSample10</c> (partition 1-2) - identical shape, the valid partition range
/// differs per operation and isn't enforced client-side (API-10).</summary>
public sealed class LikeStreamRequest
{
    /// <summary>0-5 minutes, per the registry (STREAM-09).</summary>
    public int? BackfillMinutes { get; init; }

    public required int Partition { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public XStreamLikeFieldSelection? Fields { get; init; }
}

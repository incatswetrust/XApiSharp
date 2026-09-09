using XApiSharp.Common;

namespace XApiSharp.Streaming;

/// <summary>Request for <c>GET /2/tweets/sample/stream</c> - the one volume-based Post stream
/// with no <c>partition</c>/<c>start_time</c>/<c>end_time</c> in its contract (PAGE-02: it does
/// not share <see cref="PostVolumeStreamRequest"/>'s shape).</summary>
public sealed class PostSampleStreamRequest
{
    /// <summary>0-5 minutes, per the registry (STREAM-09).</summary>
    public int? BackfillMinutes { get; init; }

    public XStreamPostFieldSelection? Fields { get; init; }
}

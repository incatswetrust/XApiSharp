using XApiSharp.Common;

namespace XApiSharp.Streaming;

/// <summary>
/// Request shared by <c>streamPostsSample10</c>, <c>streamPostsFirehose</c>, and the 4
/// <c>streamPostsFirehose*</c> language variants - identical required-parameter shape
/// (<c>partition</c> required, <c>start_time</c>/<c>end_time</c> optional) across all 6. The
/// server-side valid <see cref="Partition"/> range differs per operation (1-2 for sample10/ja/ko/pt,
/// 1-8 for the English firehose, 1-20 for the all-languages firehose) - not enforced client-side
/// (API-10), see each <see cref="StreamingClient"/> method's XML doc for its specific range.
/// </summary>
public sealed class PostVolumeStreamRequest
{
    /// <summary>0-5 minutes, per the registry (STREAM-09).</summary>
    public int? BackfillMinutes { get; init; }

    public required int Partition { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public XStreamPostFieldSelection? Fields { get; init; }
}

using XApiSharp.Common;

namespace XApiSharp.Streaming;

/// <summary>Request for <c>GET /2/tweets/search/stream</c> - the rules-filtered Post stream. Rule
/// management is separate (<see cref="StreamingClient.GetRulesAsync"/>/<see cref="StreamingClient.UpdateRulesAsync"/>).</summary>
public sealed class FilteredPostStreamRequest
{
    /// <summary>0-5 minutes, per the registry. Replays missed events on reconnect within this
    /// window - not exactly-once and not a guarantee every missed event is recovered (STREAM-09).</summary>
    public int? BackfillMinutes { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public XStreamPostFieldSelection? Fields { get; init; }
}

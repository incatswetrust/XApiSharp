using XApiSharp.Common;

namespace XApiSharp.Posts;

/// <summary>Request for <c>GET /2/tweets/analytics</c>. Up to 100 IDs, per the registry.
/// <c>StartTime</c>/<c>EndTime</c> are required (unlike almost every other time-range parameter
/// in the registry, which is optional).</summary>
public sealed class GetAnalyticsRequest
{
    public required IReadOnlyCollection<string> Ids { get; init; }

    public required DateTimeOffset StartTime { get; init; }

    public required DateTimeOffset EndTime { get; init; }

    public XAnalyticsGranularity? Granularity { get; init; }

    public IReadOnlyCollection<XAnalyticsField>? Fields { get; init; }
}

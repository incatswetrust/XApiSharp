namespace XApiSharp.Streaming;

/// <summary>
/// Request shared by <c>streamPostsCompliance</c>, <c>streamLabelsCompliance</c>,
/// <c>streamUsersCompliance</c>, <c>streamLikesCompliance</c>, and <c>activityStream</c> - all 5
/// declare the identical <c>{backfill_minutes, start_time, end_time}</c> shape, plus a required
/// <c>partition</c> (1-4) for Posts compliance and Users compliance only (PAGE-02:
/// <see cref="Partition"/> stays optional on this shared type; <see cref="StreamingClient.StreamPostsComplianceAsync"/>
/// and <see cref="StreamingClient.StreamUsersComplianceAsync"/> are the two methods that require
/// and validate it - the other 3 ignore it if set).
/// </summary>
public sealed class ComplianceStreamRequest
{
    /// <summary>0-5 minutes, per the registry (STREAM-09).</summary>
    public int? BackfillMinutes { get; init; }

    /// <summary>Required (1-4) for <see cref="StreamingClient.StreamPostsComplianceAsync"/> and
    /// <see cref="StreamingClient.StreamUsersComplianceAsync"/> only; not a parameter the other 3
    /// operations sharing this request type declare.</summary>
    public int? Partition { get; init; }

    public DateTimeOffset? StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }
}

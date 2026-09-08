using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>GET /2/broadcasts/scheduled</c>. Not paginated - the registry accepts
/// <c>pagination_token</c>/<c>max_results</c> as input but the response declares no <c>meta</c>
/// at all, same situation as <see cref="Users.GetBookmarkFoldersRequest"/>.</summary>
public sealed class ListScheduledBroadcastsRequest
{
    public int? MaxResults { get; init; }

    public DateTimeOffset? OldestStartTime { get; init; }

    public DateTimeOffset? NewestStartTime { get; init; }

    public string? PaginationToken { get; init; }
}

/// <summary>Modeled from the "ListScheduledBroadcastsResponse" schema.</summary>
public sealed class ListScheduledBroadcastsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ScheduledBroadcast>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

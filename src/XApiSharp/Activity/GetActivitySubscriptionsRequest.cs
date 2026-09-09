using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Activity;

/// <summary>
/// Request for <c>GET /2/activity/subscriptions</c>. <see cref="PaginationToken"/> exists per the
/// registry's declared query parameters, but <see cref="GetActivitySubscriptionsResponse"/> has no
/// <c>meta</c>/next-token field at all in the current contract - there is no way to obtain a
/// continuation token from a response, so this is a single-page-only operation for now (not
/// wired through <c>XPaginator</c>); pass a token yourself only if you have one from some other
/// source. Re-check the snapshot if X ever adds a <c>meta</c> object here.
/// </summary>
public sealed class GetActivitySubscriptionsRequest
{
    /// <summary>1-1000, default 1000, per the registry.</summary>
    public int? MaxResults { get; init; }

    public string? PaginationToken { get; init; }
}

/// <summary>Modeled from the "GetActivitySubscriptionsResponse" schema.</summary>
public sealed class GetActivitySubscriptionsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ActivitySubscription>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

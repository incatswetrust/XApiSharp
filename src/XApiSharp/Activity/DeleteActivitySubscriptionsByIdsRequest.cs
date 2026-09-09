using System.Text.Json.Serialization;

namespace XApiSharp.Activity;

/// <summary>Request for <c>DELETE /2/activity/subscriptions</c> - bulk delete, up to 100 IDs.</summary>
public sealed class DeleteActivitySubscriptionsByIdsRequest
{
    /// <summary>1-100 entries, per the registry.</summary>
    public required IReadOnlyCollection<string> Ids { get; init; }
}

/// <summary>
/// Modeled from the "DeleteActivitySubscriptionsByIdsResponse" schema - a genuine per-ID
/// partial-success shape (some IDs can succeed while others fail in the same call), distinct from
/// the usual single-resource <c>data</c>/<c>errors</c> pair.
/// </summary>
public sealed class DeleteActivitySubscriptionsByIdsResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<DeleteActivitySubscriptionsByIdsResult>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<DeleteActivitySubscriptionsByIdsError>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public DeleteActivitySubscriptionsByIdsMeta? Meta { get; init; }
}

public sealed class DeleteActivitySubscriptionsByIdsResult
{
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; init; }

    [JsonPropertyName("deleted")]
    public bool? Deleted { get; init; }
}

/// <summary>Per-ID failure detail, per the registry - deliberately not <see cref="Errors.XProblem"/>,
/// since this isn't the usual RFC 7807-shaped problem object, just
/// <c>{subscription_id, reason, message}</c>.</summary>
public sealed class DeleteActivitySubscriptionsByIdsError
{
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public sealed class DeleteActivitySubscriptionsByIdsMeta
{
    [JsonPropertyName("total_subscriptions")]
    public int? TotalSubscriptions { get; init; }
}

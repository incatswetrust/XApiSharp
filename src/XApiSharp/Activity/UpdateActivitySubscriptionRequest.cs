using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Activity;

/// <summary>Request for <c>PUT /2/activity/subscriptions/{subscription_id}</c> - only
/// <see cref="Tag"/>/<see cref="WebhookId"/> are updatable, per the registry; the event type and
/// filter are fixed at creation.</summary>
public sealed class UpdateActivitySubscriptionRequest
{
    public required string SubscriptionId { get; init; }

    /// <summary>1-200 characters, per the registry.</summary>
    public string? Tag { get; init; }

    public string? WebhookId { get; init; }
}

/// <summary>Modeled from the "UpdateActivitySubscriptionResponse" schema.</summary>
public sealed class UpdateActivitySubscriptionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UpdateActivitySubscriptionResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UpdateActivitySubscriptionResponseData
{
    [JsonPropertyName("subscription")]
    public ActivitySubscription? Subscription { get; init; }

    [JsonPropertyName("total_subscriptions")]
    public int? TotalSubscriptions { get; init; }
}

internal sealed class UpdateActivitySubscriptionBody
{
    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; init; }

    [JsonPropertyName("webhook_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebhookId { get; init; }
}

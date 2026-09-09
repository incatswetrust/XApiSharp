using System.Text.Json.Serialization;
using XApiSharp.Streaming;

namespace XApiSharp.Activity;

/// <summary>
/// Modeled from the "Subscription" shape the registry repeats identically (different generated
/// schema names) across <c>getActivitySubscriptions</c>'s list items and the nested
/// <c>subscription</c> object in <c>createActivitySubscription</c>/<c>updateActivitySubscription</c>'s
/// responses - one shared type instead of 3 near-identical classes.
/// </summary>
public sealed class ActivitySubscription
{
    [JsonPropertyName("subscription_id")]
    public string? SubscriptionId { get; init; }

    /// <summary>Dot-notation event type, per the registry - kept as a plain string (SER-06) like
    /// <see cref="ActivityStreamEventData.EventType"/>, not <see cref="Common.XActivityEventType"/>.</summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; init; }

    [JsonPropertyName("filter")]
    public ActivitySubscriptionFilterData? Filter { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }

    [JsonPropertyName("webhook_id")]
    public string? WebhookId { get; init; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; init; }
}

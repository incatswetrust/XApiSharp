using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Activity;

/// <summary>Request for <c>POST /2/activity/subscriptions</c>.</summary>
public sealed class CreateActivitySubscriptionRequest
{
    public required XActivityEventType EventType { get; init; }

    public required ActivitySubscriptionFilter Filter { get; init; }

    /// <summary>1-200 characters, per the registry.</summary>
    public string? Tag { get; init; }

    /// <summary>Deliver matching events to this webhook, in addition to (or instead of) reading
    /// them from <c>StreamingClient.StreamActivityAsync</c> - the registry doesn't say the two are
    /// mutually exclusive, and this SDK doesn't assume either way.</summary>
    public string? WebhookId { get; init; }
}

/// <summary>Modeled from the "CreateActivitySubscriptionResponse" schema.</summary>
public sealed class CreateActivitySubscriptionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateActivitySubscriptionResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateActivitySubscriptionResponseData
{
    [JsonPropertyName("subscription")]
    public ActivitySubscription? Subscription { get; init; }
}

internal sealed class CreateActivitySubscriptionBody
{
    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }

    [JsonPropertyName("filter")]
    public required ActivitySubscriptionFilterBody Filter { get; init; }

    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; init; }

    [JsonPropertyName("webhook_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebhookId { get; init; }
}

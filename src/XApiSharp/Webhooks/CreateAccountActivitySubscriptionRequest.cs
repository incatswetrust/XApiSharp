using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>POST /2/account_activity/webhooks/{webhook_id}/subscriptions/all</c> -
/// subscribes the authenticated user to Account Activity events on this webhook. No body, per the
/// registry (the request schema declares zero properties).</summary>
public sealed class CreateAccountActivitySubscriptionRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "CreateAccountActivitySubscriptionResponse" schema.</summary>
public sealed class CreateAccountActivitySubscriptionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateAccountActivitySubscriptionResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateAccountActivitySubscriptionResponseData
{
    [JsonPropertyName("subscribed")]
    public bool Subscribed { get; init; }
}

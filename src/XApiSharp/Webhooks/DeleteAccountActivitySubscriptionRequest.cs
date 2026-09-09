using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>DELETE /2/account_activity/webhooks/{webhook_id}/subscriptions/{user_id}/all</c>
/// - unsubscribes a user from Account Activity events on this webhook.</summary>
public sealed class DeleteAccountActivitySubscriptionRequest
{
    public required string WebhookId { get; init; }

    public required string UserId { get; init; }
}

/// <summary>Modeled from the "DeleteAccountActivitySubscriptionResponse" schema.</summary>
public sealed class DeleteAccountActivitySubscriptionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteAccountActivitySubscriptionResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

/// <summary>Modeled exactly per the registry: reports whether the user still has an active
/// subscription after the delete, not literally whether this call deleted something.</summary>
public sealed class DeleteAccountActivitySubscriptionResponseData
{
    [JsonPropertyName("subscribed")]
    public bool Subscribed { get; init; }
}

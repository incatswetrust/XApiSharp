using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>GET /2/account_activity/webhooks/{webhook_id}/subscriptions/all</c> -
/// checks whether the authenticated user has an active Account Activity subscription on this
/// webhook.</summary>
public sealed class ValidateAccountActivitySubscriptionRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "ValidateAccountActivitySubscriptionResponse" schema.</summary>
public sealed class ValidateAccountActivitySubscriptionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ValidateAccountActivitySubscriptionResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class ValidateAccountActivitySubscriptionResponseData
{
    [JsonPropertyName("subscribed")]
    public bool Subscribed { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>GET /2/account_activity/webhooks/{webhook_id}/subscriptions/all/list</c>
/// - lists every user subscribed to Account Activity events on this webhook.</summary>
public sealed class GetAccountActivitySubscriptionsRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "GetAccountActivitySubscriptionsResponse" schema.</summary>
public sealed class GetAccountActivitySubscriptionsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public GetAccountActivitySubscriptionsResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class GetAccountActivitySubscriptionsResponseData
{
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; init; }

    [JsonPropertyName("webhook_id")]
    public string? WebhookId { get; init; }

    [JsonPropertyName("webhook_url")]
    public string? WebhookUrl { get; init; }

    [JsonPropertyName("subscriptions")]
    public IReadOnlyList<AccountActivitySubscriber>? Subscriptions { get; init; }
}

public sealed class AccountActivitySubscriber
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}

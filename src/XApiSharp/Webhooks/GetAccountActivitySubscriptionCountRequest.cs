using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>GET /2/account_activity/subscriptions/count</c> - no parameters.</summary>
public sealed class GetAccountActivitySubscriptionCountRequest;

/// <summary>Modeled from the "GetAccountActivitySubscriptionCountResponse" schema.</summary>
public sealed class GetAccountActivitySubscriptionCountResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public GetAccountActivitySubscriptionCountResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class GetAccountActivitySubscriptionCountResponseData
{
    [JsonPropertyName("account_name")]
    public string? AccountName { get; init; }

    /// <summary>String on the wire per the registry, despite being a count.</summary>
    [JsonPropertyName("provisioned_count")]
    public string? ProvisionedCount { get; init; }

    /// <summary>String on the wire per the registry, despite being a count.</summary>
    [JsonPropertyName("subscriptions_count_all")]
    public string? SubscriptionsCountAll { get; init; }

    /// <summary>String on the wire per the registry, despite being a count.</summary>
    [JsonPropertyName("subscriptions_count_direct_messages")]
    public string? SubscriptionsCountDirectMessages { get; init; }
}

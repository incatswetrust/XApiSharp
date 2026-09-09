using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Activity;

/// <summary>Request for <c>DELETE /2/activity/subscriptions/{subscription_id}</c>.</summary>
public sealed class DeleteActivitySubscriptionRequest
{
    public required string SubscriptionId { get; init; }
}

/// <summary>Modeled from the "DeleteActivitySubscriptionResponse" schema.</summary>
public sealed class DeleteActivitySubscriptionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteActivitySubscriptionResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteActivitySubscriptionResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

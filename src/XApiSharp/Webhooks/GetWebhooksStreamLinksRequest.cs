using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>GET /2/tweets/search/webhooks</c> - no parameters.</summary>
public sealed class GetWebhooksStreamLinksRequest;

/// <summary>Modeled from the "GetWebhooksStreamLinksResponse" schema.</summary>
public sealed class GetWebhooksStreamLinksResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<WebhookStreamLink>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class WebhookStreamLink
{
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; init; }

    [JsonPropertyName("business_user_id")]
    public string? BusinessUserId { get; init; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyList<string>? Fields { get; init; }

    [JsonPropertyName("instance_id")]
    public string? InstanceId { get; init; }

    [JsonPropertyName("webhook_id")]
    public string? WebhookId { get; init; }
}

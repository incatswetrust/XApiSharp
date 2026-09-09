using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>GET /2/webhooks</c>.</summary>
public sealed class GetWebhooksRequest
{
    public IReadOnlyCollection<XWebhookConfigField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetWebhooksResponse" schema.</summary>
public sealed class GetWebhooksResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<WebhookConfig>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public GetWebhooksMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class GetWebhooksMeta
{
    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

public sealed class WebhookConfig
{
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("valid")]
    public bool? Valid { get; init; }
}

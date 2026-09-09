using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>POST /2/tweets/search/webhooks/{webhook_id}</c> - links the filtered
/// Post stream to a registered webhook. No body, per the registry.</summary>
public sealed class CreateWebhooksStreamLinkRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "CreateWebhooksStreamLinkResponse" schema.</summary>
public sealed class CreateWebhooksStreamLinkResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateWebhooksStreamLinkResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateWebhooksStreamLinkResponseData
{
    [JsonPropertyName("provisioned")]
    public bool Provisioned { get; init; }
}

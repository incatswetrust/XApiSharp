using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>DELETE /2/tweets/search/webhooks/{webhook_id}</c>.</summary>
public sealed class DeleteWebhooksStreamLinkRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "DeleteWebhooksStreamLinkResponse" schema.</summary>
public sealed class DeleteWebhooksStreamLinkResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteWebhooksStreamLinkResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteWebhooksStreamLinkResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

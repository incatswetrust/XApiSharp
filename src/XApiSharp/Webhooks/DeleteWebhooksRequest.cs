using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>DELETE /2/webhooks/{webhook_id}</c>.</summary>
public sealed class DeleteWebhooksRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "DeleteWebhooksResponse" schema.</summary>
public sealed class DeleteWebhooksResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteWebhooksResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteWebhooksResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

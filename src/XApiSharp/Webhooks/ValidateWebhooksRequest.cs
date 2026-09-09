using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>PUT /2/webhooks/{webhook_id}</c> - asks X to re-trigger the CRC
/// challenge against the registered URL (see <see cref="XWebhookChallengeResponder"/>). Not a
/// local signature check - <see cref="ValidateWebhooksResponseData.Valid"/> only reports whether
/// X triggered the check, per its own description in the registry, not whether it passed.</summary>
public sealed class ValidateWebhooksRequest
{
    public required string WebhookId { get; init; }
}

/// <summary>Modeled from the "ValidateWebhooksResponse" schema.</summary>
public sealed class ValidateWebhooksResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ValidateWebhooksResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class ValidateWebhooksResponseData
{
    [JsonPropertyName("valid")]
    public bool Valid { get; init; }
}

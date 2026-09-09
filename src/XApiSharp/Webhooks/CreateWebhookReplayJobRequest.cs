using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>
/// Request for <c>POST /2/webhooks/replay</c> - redelivers events for a webhook over a time
/// range as a background job (spec section 17.2 applies: persist <see cref="CreateWebhookReplayJobResponseData.JobId"/>,
/// don't resubmit on an unknown outcome, don't invent a cancel/delete operation the registry
/// doesn't declare). <see cref="FromDate"/>/<see cref="ToDate"/> are truncated to the minute and
/// rendered as UTC <c>yyyyMMddHHmm</c> (12 digits), per the registry's pattern.
/// </summary>
public sealed class CreateWebhookReplayJobRequest
{
    public required string WebhookId { get; init; }

    public required DateTimeOffset FromDate { get; init; }

    public required DateTimeOffset ToDate { get; init; }
}

/// <summary>Modeled from the "CreateWebhookReplayJobResponse" schema.</summary>
public sealed class CreateWebhookReplayJobResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateWebhookReplayJobResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateWebhookReplayJobResponseData
{
    [JsonPropertyName("job_id")]
    public required string JobId { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }
}

internal sealed class CreateWebhookReplayJobBody
{
    [JsonPropertyName("webhook_id")]
    public required string WebhookId { get; init; }

    [JsonPropertyName("from_date")]
    public required string FromDate { get; init; }

    [JsonPropertyName("to_date")]
    public required string ToDate { get; init; }
}

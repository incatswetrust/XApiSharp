using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Webhooks;

/// <summary>Request for <c>POST /2/webhooks</c> - registers a URL to receive webhook events. X
/// delivers a CRC challenge to <see cref="Url"/> as part of registration; your endpoint must
/// answer it correctly (see <see cref="XWebhookChallengeResponder"/>) or the webhook is created
/// with <c>valid: false</c>.</summary>
public sealed class CreateWebhooksRequest
{
    /// <summary>1-200 characters, per the registry.</summary>
    public required string Url { get; init; }
}

/// <summary>Modeled from the "CreateWebhooksResponse" schema.</summary>
public sealed class CreateWebhooksResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateWebhooksResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateWebhooksResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("valid")]
    public bool Valid { get; init; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; init; }
}

internal sealed class CreateWebhooksBody
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

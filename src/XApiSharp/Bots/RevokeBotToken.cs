using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Bots;

/// <summary>Request for <c>DELETE /2/bots/{id}/token</c>.</summary>
public sealed class RevokeBotTokenRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "RevokeBotTokenResponse" schema.</summary>
public sealed class RevokeBotTokenResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public RevokeBotTokenResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class RevokeBotTokenResponseData
{
    [JsonPropertyName("revoked")]
    public bool Revoked { get; init; }
}

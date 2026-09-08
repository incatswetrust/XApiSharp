using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Bots;

/// <summary>Request for <c>POST /2/bots/{id}/token</c> - invalidates every previously issued
/// token for this bot, per the registry.</summary>
public sealed class RotateBotTokenRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<string>? Scopes { get; init; }
}

/// <summary>Modeled from the "RotateBotTokenResponse" schema.</summary>
public sealed class RotateBotTokenResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public BotToken? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class RotateBotTokenBody
{
    [JsonPropertyName("scopes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? Scopes { get; init; }
}

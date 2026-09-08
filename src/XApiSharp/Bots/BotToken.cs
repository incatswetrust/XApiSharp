using System.Text.Json.Serialization;

namespace XApiSharp.Bots;

/// <summary>Shared shape for "CreateBotResponseData" and "RotateBotTokenResponseData" - both
/// identical in the registry: a newly (re)issued bearer token, returned exactly once.</summary>
public sealed class BotToken
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>Returned exactly once - store it securely. Not retrievable again; a forgotten
    /// token means rotating (<c>RotateBotTokenAsync</c>) to get a new one.</summary>
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("scopes")]
    public IReadOnlyList<string>? Scopes { get; init; }

    [JsonPropertyName("token_expires_at_ms")]
    public long? TokenExpiresAtMs { get; init; }
}

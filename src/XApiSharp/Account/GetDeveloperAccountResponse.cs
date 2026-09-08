using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Account;

/// <summary>Modeled from the "GetDeveloperAccountResponse" schema.</summary>
public sealed class GetDeveloperAccountResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeveloperAccount? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeveloperAccount
{
    [JsonPropertyName("account_id")]
    public required string AccountId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("created_at")]
    public required DateTimeOffset CreatedAt { get; init; }
}

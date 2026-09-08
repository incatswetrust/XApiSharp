using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Usage;

/// <summary>Modeled from the "GetUsageCreditsResponse" schema.</summary>
public sealed class GetUsageCreditsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UsageCredits? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UsageCredits
{
    /// <summary>Spendable USD remaining, clamped at 0.00: <c>max(0, PrepaidBalance + FreeBalance)</c>.</summary>
    [JsonPropertyName("total_balance")]
    public double TotalBalance { get; init; }

    /// <summary>Purchased (prepaid) USD balance. May be negative when paid usage has overdrawn
    /// this bucket.</summary>
    [JsonPropertyName("prepaid_balance")]
    public double PrepaidBalance { get; init; }

    [JsonPropertyName("free_balance")]
    public double FreeBalance { get; init; }

    [JsonPropertyName("free_grants")]
    public IReadOnlyList<UsageFreeGrant>? FreeGrants { get; init; }
}

public sealed class UsageFreeGrant
{
    [JsonPropertyName("amount")]
    public double Amount { get; init; }

    /// <summary>Omitted if the grant never expires or the expiry is unknown, per the registry.</summary>
    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

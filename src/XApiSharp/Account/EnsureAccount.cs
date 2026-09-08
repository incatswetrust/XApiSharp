using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Account;

/// <summary>Request for <c>POST /2/account</c> - creates the developer account for the
/// authenticated user if one doesn't already exist (idempotent, per <c>Created</c> in the
/// response distinguishing "just created" from "already existed").</summary>
public sealed class EnsureAccountRequest
{
    /// <summary>Up to 64 characters, per the registry. Opaque caller-supplied metadata.</summary>
    public string? Metadata { get; init; }
}

/// <summary>Modeled from the "EnsureAccountResponse" schema.</summary>
public sealed class EnsureAccountResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public EnsureAccountResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class EnsureAccountResponseData
{
    [JsonPropertyName("account_id")]
    public required string AccountId { get; init; }

    [JsonPropertyName("created")]
    public bool Created { get; init; }
}

internal sealed class EnsureAccountBody
{
    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Metadata { get; init; }
}

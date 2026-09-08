using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/dm/unblock</c>. No request body - the target is the
/// path parameter.</summary>
public sealed class UnblockUserDmsRequest
{
    public required string TargetUserId { get; init; }
}

/// <summary>Modeled from the "UnblockUsersDmsResponse" schema.</summary>
public sealed class UnblockUserDmsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnblockUserDmsResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnblockUserDmsResponseData
{
    [JsonPropertyName("blocked")]
    public bool Blocked { get; init; }
}

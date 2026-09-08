using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/dm/block</c> - blocks <see cref="TargetUserId"/>
/// from sending the authenticated user Direct Messages. No request body - the target is the path
/// parameter.</summary>
public sealed class BlockUserDmsRequest
{
    public required string TargetUserId { get; init; }
}

/// <summary>Modeled from the "BlockUsersDmsResponse" schema.</summary>
public sealed class BlockUserDmsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public BlockUserDmsResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class BlockUserDmsResponseData
{
    [JsonPropertyName("blocked")]
    public bool Blocked { get; init; }
}

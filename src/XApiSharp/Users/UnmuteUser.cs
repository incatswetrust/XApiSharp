using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{source_user_id}/muting/{target_user_id}</c>.</summary>
public sealed class UnmuteUserRequest
{
    public required string SourceUserId { get; init; }

    public required string TargetUserId { get; init; }
}

/// <summary>Modeled from the "UnmuteUserResponse" schema.</summary>
public sealed class UnmuteUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnmuteUserResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnmuteUserResponseData
{
    [JsonPropertyName("muting")]
    public bool Muting { get; init; }
}

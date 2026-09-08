using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/muting</c> - <see cref="SourceUserId"/> mutes
/// <see cref="TargetUserId"/>.</summary>
public sealed class MuteUserRequest
{
    public required string SourceUserId { get; init; }

    public required string TargetUserId { get; init; }
}

/// <summary>Modeled from the "MuteUserResponse" schema.</summary>
public sealed class MuteUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MuteUserResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class MuteUserResponseData
{
    [JsonPropertyName("muting")]
    public bool Muting { get; init; }
}

internal sealed class MuteUserBody
{
    [JsonPropertyName("target_user_id")]
    public required string TargetUserId { get; init; }
}

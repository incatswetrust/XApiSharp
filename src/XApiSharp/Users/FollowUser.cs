using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/following</c> - <see cref="SourceUserId"/> follows
/// <see cref="TargetUserId"/>.</summary>
public sealed class FollowUserRequest
{
    public required string SourceUserId { get; init; }

    public required string TargetUserId { get; init; }
}

/// <summary>Modeled from the "FollowUserResponse" schema.</summary>
public sealed class FollowUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public FollowUserResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class FollowUserResponseData
{
    [JsonPropertyName("following")]
    public bool Following { get; init; }

    [JsonPropertyName("pending_follow")]
    public bool PendingFollow { get; init; }
}

/// <summary>Wire shape of the JSON request body - "FollowUserRequest" in the registry.</summary>
internal sealed class FollowUserBody
{
    [JsonPropertyName("target_user_id")]
    public required string TargetUserId { get; init; }
}

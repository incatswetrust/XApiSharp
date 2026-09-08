using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{source_user_id}/following/{target_user_id}</c>.</summary>
public sealed class UnfollowUserRequest
{
    public required string SourceUserId { get; init; }

    public required string TargetUserId { get; init; }
}

/// <summary>Modeled from the "UnfollowUserResponse" schema.</summary>
public sealed class UnfollowUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnfollowUserResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnfollowUserResponseData
{
    [JsonPropertyName("following")]
    public bool Following { get; init; }
}

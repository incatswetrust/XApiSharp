using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>DELETE /2/broadcasts/{id}/chat/mutes/{user_id}</c>.</summary>
public sealed class UnmuteBroadcastChatUserRequest
{
    public required string Id { get; init; }

    public required string UserId { get; init; }
}

/// <summary>Modeled from the "UnmuteBroadcastChatUserResponse" schema.</summary>
public sealed class UnmuteBroadcastChatUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnmuteBroadcastChatUserResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnmuteBroadcastChatUserResponseData
{
    [JsonPropertyName("muted")]
    public bool Muted { get; init; }
}

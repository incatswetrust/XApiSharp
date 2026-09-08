using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>POST /2/broadcasts/{id}/chat/mutes</c> - mutes or times out a user in
/// the broadcast's chat.</summary>
public sealed class MuteBroadcastChatUserRequest
{
    public required string Id { get; init; }

    public required string UserId { get; init; }

    /// <summary>Timeout expiry, epoch milliseconds - omit for an indefinite mute, per the
    /// registry.</summary>
    public string? EndAtMs { get; init; }

    /// <summary>The specific message that triggered the mute, if any.</summary>
    public string? MessageId { get; init; }
}

/// <summary>Modeled from the "MuteBroadcastChatUserResponse" schema.</summary>
public sealed class MuteBroadcastChatUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MuteBroadcastChatUserResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class MuteBroadcastChatUserResponseData
{
    [JsonPropertyName("muted")]
    public bool Muted { get; init; }
}

internal sealed class MuteBroadcastChatUserBody
{
    [JsonPropertyName("user_id")]
    public required string UserId { get; init; }

    [JsonPropertyName("end_at_ms")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EndAtMs { get; init; }

    [JsonPropertyName("message_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/media/upload/{id}/finalize</c>.</summary>
public sealed class ChatMediaUploadFinalizeRequest
{
    /// <summary>The upload session ID - the <c>{id}</c> path segment.</summary>
    public required string SessionId { get; init; }

    public required string ConversationId { get; init; }

    public required string MediaHashKey { get; init; }

    /// <summary>Total number of segments sent, per the registry (sent as a string on the wire).</summary>
    public required int NumParts { get; init; }

    public string? MessageId { get; init; }

    /// <summary>Media time-to-live in milliseconds, per the registry (sent as a string on the
    /// wire).</summary>
    public long? TtlMilliseconds { get; init; }
}

/// <summary>Modeled from the "ChatMediaUploadFinalizeResponse" schema.</summary>
public sealed class ChatMediaUploadFinalizeResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ChatMediaUploadFinalizeResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class ChatMediaUploadFinalizeResponseData
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

internal sealed class ChatMediaUploadFinalizeBody
{
    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("media_hash_key")]
    public required string MediaHashKey { get; init; }

    [JsonPropertyName("num_parts")]
    public required string NumParts { get; init; }

    [JsonPropertyName("message_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MessageId { get; init; }

    [JsonPropertyName("ttl_msec")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TtlMsec { get; init; }
}

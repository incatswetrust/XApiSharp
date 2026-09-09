using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/media/upload/initialize</c> - the first step of Chat's own
/// chunked media upload sequence (distinct from the Media family's - a different endpoint tree,
/// scoped to one conversation).</summary>
public sealed class ChatMediaUploadInitializeRequest
{
    public required string ConversationId { get; init; }

    public required long TotalBytes { get; init; }
}

/// <summary>Modeled from the "ChatMediaUploadInitializeResponse" schema.</summary>
public sealed class ChatMediaUploadInitializeResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ChatMediaUploadInitializeResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class ChatMediaUploadInitializeResponseData
{
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }

    [JsonPropertyName("media_hash_key")]
    public required string MediaHashKey { get; init; }

    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }
}

internal sealed class ChatMediaUploadInitializeBody
{
    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("total_bytes")]
    public required long TotalBytes { get; init; }
}

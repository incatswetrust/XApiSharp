using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/{id}/read</c>.</summary>
public sealed class MarkChatConversationReadRequest
{
    public required string ConversationId { get; init; }

    public required string SeenUntilSequenceId { get; init; }
}

/// <summary>Modeled from the "MarkChatConversationReadResponse" schema.</summary>
public sealed class MarkChatConversationReadResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MarkChatConversationReadResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class MarkChatConversationReadResponseData
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

internal sealed class MarkChatConversationReadBody
{
    [JsonPropertyName("seen_until_sequence_id")]
    public required string SeenUntilSequenceId { get; init; }
}

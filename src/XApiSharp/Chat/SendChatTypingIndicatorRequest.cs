using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/{id}/typing</c> - no body, per the registry.</summary>
public sealed class SendChatTypingIndicatorRequest
{
    public required string ConversationId { get; init; }
}

/// <summary>Modeled from the "SendChatTypingIndicatorResponse" schema.</summary>
public sealed class SendChatTypingIndicatorResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public SendChatTypingIndicatorResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class SendChatTypingIndicatorResponseData
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

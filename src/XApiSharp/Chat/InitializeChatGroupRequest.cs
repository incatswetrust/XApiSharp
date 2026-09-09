using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/group/initialize</c> - no body, per the
/// registry. The typical first step before <see cref="CreateChatConversationRequest"/>: reserves a
/// conversation ID for the caller's own end-to-end encryption setup to key against.</summary>
public sealed class InitializeChatGroupRequest;

/// <summary>Modeled from the "InitializeChatGroupResponse" schema.</summary>
public sealed class InitializeChatGroupResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public InitializeChatGroupResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class InitializeChatGroupResponseData
{
    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }
}

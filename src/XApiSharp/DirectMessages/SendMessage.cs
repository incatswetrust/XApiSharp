namespace XApiSharp.DirectMessages;

/// <summary>Request for <c>POST /2/dm_conversations/with/{participant_id}/messages</c>. See
/// <see cref="DirectMessageContent"/> for the shared text/media-attachment shape.</summary>
public sealed class SendToParticipantRequest
{
    public required string ParticipantId { get; init; }

    public required DirectMessageContent Message { get; init; }
}

/// <summary>Request for <c>POST /2/dm_conversations/{dm_conversation_id}/messages</c>.</summary>
public sealed class SendToConversationRequest
{
    public required string DmConversationId { get; init; }

    public required DirectMessageContent Message { get; init; }
}

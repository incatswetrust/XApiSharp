using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.DirectMessages;

/// <summary>Request for <c>POST /2/dm_conversations</c> - creates a new group conversation (2-49
/// participants) with an initial message. The registry's only documented
/// <c>conversation_type</c> value is <c>"Group"</c> - there's no way to create a 1:1 conversation
/// through this operation; sending to a specific participant who has no existing conversation is
/// <see cref="DirectMessagesClient.SendToParticipantAsync"/> instead.</summary>
public sealed class CreateConversationRequest
{
    public required IReadOnlyCollection<string> ParticipantIds { get; init; }

    public required DirectMessageContent Message { get; init; }
}

/// <summary>Shared message shape - at least one of <see cref="Text"/>/<see cref="MediaIds"/> is
/// required, per the registry's <c>anyOf</c> (used identically by
/// <see cref="CreateConversationRequest"/> and both send-message operations).</summary>
public sealed class DirectMessageContent
{
    public string? Text { get; init; }

    public IReadOnlyCollection<string>? MediaIds { get; init; }
}

/// <summary>Modeled from the "CreateDirectMessagesConversationResponse" schema. Returned with
/// HTTP 201.</summary>
public sealed class DirectMessageSendResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DirectMessageSendResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DirectMessageSendResponseData
{
    [JsonPropertyName("dm_conversation_id")]
    public required string DmConversationId { get; init; }

    [JsonPropertyName("dm_event_id")]
    public required string DmEventId { get; init; }
}

internal sealed class CreateConversationBody
{
    [JsonPropertyName("conversation_type")]
    public string ConversationType { get; init; } = "Group";

    [JsonPropertyName("participant_ids")]
    public required IReadOnlyCollection<string> ParticipantIds { get; init; }

    [JsonPropertyName("message")]
    public required DirectMessageContentBody Message { get; init; }
}

internal sealed class DirectMessageContentBody
{
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    [JsonPropertyName("attachments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<DirectMessageAttachmentBody>? Attachments { get; init; }
}

internal sealed class DirectMessageAttachmentBody
{
    [JsonPropertyName("media_id")]
    public required string MediaId { get; init; }
}

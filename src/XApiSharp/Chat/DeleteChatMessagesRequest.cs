using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/{id}/messages/delete</c>.</summary>
public sealed class DeleteChatMessagesRequest
{
    public required string ConversationId { get; init; }

    /// <summary>At least 1 entry, per the registry.</summary>
    public required IReadOnlyCollection<string> SequenceIds { get; init; }

    public required XChatDeleteMessageAction DeleteMessageAction { get; init; }

    public required IReadOnlyCollection<ChatActionSignature> ActionSignatures { get; init; }

    public IReadOnlyCollection<string>? MediaHashKeys { get; init; }
}

/// <summary>Modeled from the "DeleteChatMessagesResponse" schema.</summary>
public sealed class DeleteChatMessagesResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteChatMessagesResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteChatMessagesResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

internal sealed class DeleteChatMessagesBody
{
    [JsonPropertyName("sequence_ids")]
    public required IReadOnlyCollection<string> SequenceIds { get; init; }

    [JsonPropertyName("delete_message_action")]
    public required string DeleteMessageAction { get; init; }

    [JsonPropertyName("action_signatures")]
    public required IReadOnlyCollection<ChatActionSignatureBody> ActionSignatures { get; init; }

    [JsonPropertyName("media_hash_keys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? MediaHashKeys { get; init; }
}

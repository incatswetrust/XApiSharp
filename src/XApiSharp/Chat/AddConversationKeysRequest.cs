using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/{id}/keys</c>. Spec section 3.2: key
/// material stays opaque, caller-produced (see <see cref="ChatConversationParticipantKey"/>).</summary>
public sealed class AddConversationKeysRequest
{
    public required string ConversationId { get; init; }

    public required string ConversationKeyVersion { get; init; }

    public required IReadOnlyCollection<ChatConversationParticipantKey> ConversationParticipantKeys { get; init; }

    public IReadOnlyCollection<ChatActionSignature>? ActionSignatures { get; init; }

    public string? Base64EncodedKeyRotation { get; init; }
}

/// <summary>Modeled from the "AddConversationKeysResponse" schema.</summary>
public sealed class AddConversationKeysResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public AddConversationKeysResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class AddConversationKeysResponseData
{
    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; init; }

    [JsonPropertyName("sequence_id")]
    public string? SequenceId { get; init; }
}

internal sealed class AddConversationKeysBody
{
    [JsonPropertyName("conversation_key_version")]
    public required string ConversationKeyVersion { get; init; }

    [JsonPropertyName("conversation_participant_keys")]
    public required IReadOnlyCollection<ChatConversationParticipantKeyBody> ConversationParticipantKeys { get; init; }

    [JsonPropertyName("action_signatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ChatActionSignatureBody>? ActionSignatures { get; init; }

    [JsonPropertyName("base64_encoded_key_rotation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Base64EncodedKeyRotation { get; init; }
}

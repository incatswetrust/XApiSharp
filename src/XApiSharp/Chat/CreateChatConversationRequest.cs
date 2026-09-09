using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>
/// Request for <c>POST /2/chat/conversations/group</c>. Spec section 3.2: every key/signature
/// field here is opaque, caller-produced material from your own end-to-end encryption flow (see
/// <see cref="InitializeChatGroupResponseData"/> for the conversation ID this typically starts
/// from) - the SDK transports it as-is and performs none of the cryptography itself.
/// </summary>
public sealed class CreateChatConversationRequest
{
    public required string ConversationId { get; init; }

    public required string ConversationKeyVersion { get; init; }

    public required IReadOnlyCollection<ChatConversationParticipantKey> ConversationParticipantKeys { get; init; }

    public required IReadOnlyCollection<string> GroupMembers { get; init; }

    public IReadOnlyCollection<ChatActionSignature>? ActionSignatures { get; init; }

    public string? Base64EncodedKeyRotation { get; init; }

    public IReadOnlyCollection<string>? GroupAdmins { get; init; }

    public string? GroupAvatarUrl { get; init; }

    public string? GroupDescription { get; init; }

    public string? GroupName { get; init; }

    /// <summary>Message time-to-live in milliseconds, per the registry (sent as a string on the
    /// wire).</summary>
    public long? TtlMilliseconds { get; init; }
}

/// <summary>Modeled from the "CreateChatConversationResponse" schema.</summary>
public sealed class CreateChatConversationResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateChatConversationResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateChatConversationResponseData
{
    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("conversation_key_change_sequence_id")]
    public string? ConversationKeyChangeSequenceId { get; init; }
}

internal sealed class CreateChatConversationBody
{
    [JsonPropertyName("conversation_id")]
    public required string ConversationId { get; init; }

    [JsonPropertyName("conversation_key_version")]
    public required string ConversationKeyVersion { get; init; }

    [JsonPropertyName("conversation_participant_keys")]
    public required IReadOnlyCollection<ChatConversationParticipantKeyBody> ConversationParticipantKeys { get; init; }

    [JsonPropertyName("group_members")]
    public required IReadOnlyCollection<string> GroupMembers { get; init; }

    [JsonPropertyName("action_signatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ChatActionSignatureBody>? ActionSignatures { get; init; }

    [JsonPropertyName("base64_encoded_key_rotation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Base64EncodedKeyRotation { get; init; }

    [JsonPropertyName("group_admins")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? GroupAdmins { get; init; }

    [JsonPropertyName("group_avatar_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupAvatarUrl { get; init; }

    [JsonPropertyName("group_description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupDescription { get; init; }

    [JsonPropertyName("group_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupName { get; init; }

    [JsonPropertyName("ttl_msec")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TtlMsec { get; init; }
}

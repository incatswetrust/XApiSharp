using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/chat/conversations/{id}/members</c>.</summary>
public sealed class AddChatGroupMembersRequest
{
    public required string ConversationId { get; init; }

    public required IReadOnlyCollection<string> UserIds { get; init; }

    public IReadOnlyCollection<ChatActionSignature>? ActionSignatures { get; init; }

    public string? ConversationKeyVersion { get; init; }

    public IReadOnlyCollection<ChatConversationParticipantKey>? ConversationParticipantKeys { get; init; }

    public string? EncryptedAvatarUrl { get; init; }

    public string? EncryptedTitle { get; init; }
}

/// <summary>
/// Modeled from the "AddChatGroupMembersResponse" schema. Its <c>data</c> is declared in the
/// registry as a <c>Post</c> object (the same schema regular Posts use), not a
/// membership/conversation shape - implemented literally per the contract rather than guessing a
/// more sensible shape (API-10); this looks like it may be an artifact upstream, not something
/// this SDK invents an alternative for.
/// </summary>
public sealed class AddChatGroupMembersResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public Post? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class AddChatGroupMembersBody
{
    [JsonPropertyName("user_ids")]
    public required IReadOnlyCollection<string> UserIds { get; init; }

    [JsonPropertyName("action_signatures")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ChatActionSignatureBody>? ActionSignatures { get; init; }

    [JsonPropertyName("conversation_key_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConversationKeyVersion { get; init; }

    [JsonPropertyName("conversation_participant_keys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ChatConversationParticipantKeyBody>? ConversationParticipantKeys { get; init; }

    [JsonPropertyName("encrypted_avatar_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncryptedAvatarUrl { get; init; }

    [JsonPropertyName("encrypted_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncryptedTitle { get; init; }
}

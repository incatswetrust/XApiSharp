using System.Text.Json.Serialization;

namespace XApiSharp.Chat;

/// <summary>
/// Modeled from the "ConversationParticipantKeys" shape the registry repeats identically across
/// <c>createChatConversation</c>/<c>addConversationKeys</c>/<c>addChatGroupMembers</c> - one
/// shared type rather than 3 near-identical classes. Spec section 3.2: the encrypted key material
/// is opaque, caller-produced (or caller-forwarded) ciphertext - carried as a plain string, never
/// generated or decrypted by the SDK.
/// </summary>
public sealed class ChatConversationParticipantKey
{
    public string? UserId { get; init; }

    /// <summary>The conversation key, encrypted with this participant's public key.</summary>
    public string? EncryptedConversationKey { get; init; }

    public string? PublicKeyVersion { get; init; }
}

internal sealed class ChatConversationParticipantKeyBody
{
    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserId { get; init; }

    [JsonPropertyName("encrypted_conversation_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EncryptedConversationKey { get; init; }

    [JsonPropertyName("public_key_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicKeyVersion { get; init; }
}

internal static class ChatConversationParticipantKeyMapper
{
    public static IReadOnlyCollection<ChatConversationParticipantKeyBody>? ToBody(IReadOnlyCollection<ChatConversationParticipantKey>? keys) =>
        keys?.Select(k => new ChatConversationParticipantKeyBody
        {
            UserId = k.UserId,
            EncryptedConversationKey = k.EncryptedConversationKey,
            PublicKeyVersion = k.PublicKeyVersion,
        }).ToList();
}

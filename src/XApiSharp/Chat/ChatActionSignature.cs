using System.Text.Json.Serialization;

namespace XApiSharp.Chat;

/// <summary>
/// Modeled from the "ActionSignatures" shape the registry repeats identically across
/// <c>createChatConversation</c>/<c>addConversationKeys</c>/<c>addChatGroupMembers</c>/
/// <c>deleteChatMessages</c> (each with its own generated schema name, but structurally the same
/// fields - one shared type here rather than 4 near-identical classes). Spec section 3.2: every
/// field here is opaque, caller-produced key/signature material - the SDK carries it as plain
/// strings and never generates, decodes, or re-derives any of it itself.
/// </summary>
public sealed class ChatActionSignature
{
    /// <summary>Client-generated ID of the message being signed.</summary>
    public required string MessageId { get; init; }

    /// <summary>Base64-encoded message event detail, produced by the caller's own signing flow.</summary>
    public required string EncodedMessageEventDetail { get; init; }

    public required ChatMessageEventSignature MessageEventSignature { get; init; }

    /// <summary>Payload string the client signed - per the registry, used only in server-side
    /// failure logs.</summary>
    public string? SignaturePayload { get; init; }
}

public sealed class ChatMessageEventSignature
{
    public required string Signature { get; init; }

    public required string PublicKeyVersion { get; init; }

    public required string SignatureVersion { get; init; }

    public string? SigningPublicKey { get; init; }

    public IReadOnlyCollection<ChatMessageSigningKeyInfo>? MessageSigningKeyInfoList { get; init; }
}

public sealed class ChatMessageSigningKeyInfo
{
    public string? MemberId { get; init; }

    public string? PublicKeyVersion { get; init; }

    public string? SigningPublicKey { get; init; }
}

internal sealed class ChatActionSignatureBody
{
    [JsonPropertyName("message_id")]
    public required string MessageId { get; init; }

    [JsonPropertyName("encoded_message_event_detail")]
    public required string EncodedMessageEventDetail { get; init; }

    [JsonPropertyName("message_event_signature")]
    public required ChatMessageEventSignatureBody MessageEventSignature { get; init; }

    [JsonPropertyName("signature_payload")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignaturePayload { get; init; }
}

internal sealed class ChatMessageEventSignatureBody
{
    [JsonPropertyName("signature")]
    public required string Signature { get; init; }

    [JsonPropertyName("public_key_version")]
    public required string PublicKeyVersion { get; init; }

    [JsonPropertyName("signature_version")]
    public required string SignatureVersion { get; init; }

    [JsonPropertyName("signing_public_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SigningPublicKey { get; init; }

    [JsonPropertyName("message_signing_key_info_list")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<ChatMessageSigningKeyInfoBody>? MessageSigningKeyInfoList { get; init; }
}

internal sealed class ChatMessageSigningKeyInfoBody
{
    [JsonPropertyName("member_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MemberId { get; init; }

    [JsonPropertyName("public_key_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicKeyVersion { get; init; }

    [JsonPropertyName("signing_public_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SigningPublicKey { get; init; }
}

/// <summary>Maps the public <see cref="ChatActionSignature"/> request type to its wire body -
/// shared by every operation that accepts <c>action_signatures</c>, instead of repeating the same
/// four-level mapping in each client method.</summary>
internal static class ChatActionSignatureMapper
{
    public static IReadOnlyCollection<ChatActionSignatureBody>? ToBody(IReadOnlyCollection<ChatActionSignature>? signatures) =>
        signatures?.Select(s => new ChatActionSignatureBody
        {
            MessageId = s.MessageId,
            EncodedMessageEventDetail = s.EncodedMessageEventDetail,
            SignaturePayload = s.SignaturePayload,
            MessageEventSignature = new ChatMessageEventSignatureBody
            {
                Signature = s.MessageEventSignature.Signature,
                PublicKeyVersion = s.MessageEventSignature.PublicKeyVersion,
                SignatureVersion = s.MessageEventSignature.SignatureVersion,
                SigningPublicKey = s.MessageEventSignature.SigningPublicKey,
                MessageSigningKeyInfoList = s.MessageEventSignature.MessageSigningKeyInfoList?
                    .Select(k => new ChatMessageSigningKeyInfoBody
                    {
                        MemberId = k.MemberId,
                        PublicKeyVersion = k.PublicKeyVersion,
                        SigningPublicKey = k.SigningPublicKey,
                    })
                    .ToList(),
            },
        }).ToList();
}

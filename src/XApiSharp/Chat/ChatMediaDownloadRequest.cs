namespace XApiSharp.Chat;

/// <summary>
/// Request for <c>GET /2/chat/media/{id}/{media_hash_key}</c> - not JSON (spec section 9), returns
/// raw bytes. Spec section 3.2: the returned bytes are the ciphertext/attachment exactly as
/// stored - the SDK does not decrypt or interpret them.
/// </summary>
public sealed class ChatMediaDownloadRequest
{
    /// <summary>Conversation ID the media belongs to.</summary>
    public required string Id { get; init; }

    public required string MediaHashKey { get; init; }
}

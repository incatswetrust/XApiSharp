using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>
/// Request for <c>POST /2/chat/media/upload/{id}/append</c> - one already-read, bounded segment.
/// <see cref="Segment"/> is a buffered <c>byte[]</c>, not a <see cref="System.IO.Stream"/>,
/// deliberately - an exact-byte retry of a segment needs the same bytes resent, which a live
/// stream can't guarantee (same reasoning as <c>AppendMediaUploadRequest</c> in the Media family).
/// The registry declares both an <c>application/json</c> and a <c>multipart/form-data</c> body
/// shape for this operation; this SDK sends multipart, consistent with the rest of its
/// chunked-upload transport.
/// </summary>
public sealed class ChatMediaUploadAppendRequest
{
    /// <summary>The upload session ID from <see cref="ChatMediaUploadInitializeResponseData.SessionId"/>
    /// - the <c>{id}</c> path segment.</summary>
    public required string SessionId { get; init; }

    public required string ConversationId { get; init; }

    public required string MediaHashKey { get; init; }

    /// <summary>0-999, per the registry.</summary>
    public required int SegmentIndex { get; init; }

    public required byte[] Segment { get; init; }
}

/// <summary>Modeled from the "ChatMediaUploadAppendResponse" schema.</summary>
public sealed class ChatMediaUploadAppendResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ChatMediaUploadAppendResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class ChatMediaUploadAppendResponseData
{
    /// <summary>Epoch seconds when the upload session expires, per the registry.</summary>
    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; init; }
}

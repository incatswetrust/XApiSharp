namespace XApiSharp.DirectMessages;

/// <summary>Request for <c>GET /2/dm_conversations/media/{dm_id}/{media_id}/{resource_id}</c> -
/// downloads DM media as raw bytes (<c>application/octet-stream</c> in the registry, not JSON;
/// spec section 9's "не все ответы приводятся к схеме data/meta").</summary>
public sealed class DownloadMediaRequest
{
    public required string DmId { get; init; }

    public required string MediaId { get; init; }

    public required string ResourceId { get; init; }
}

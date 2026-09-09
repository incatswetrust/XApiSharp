namespace XApiSharp.Media;

/// <summary>
/// Low-level request for <c>POST /2/media/upload/{id}/append</c> - one already-read, bounded
/// segment (spec section 15.1, step 3). <see cref="Segment"/> is a buffered
/// <c>byte[]</c>, not a <see cref="Stream"/>, deliberately - MEDIA-05 requires an exact-byte
/// retry of a segment, which a live (possibly non-seekable) stream can't guarantee; buffering one
/// bounded segment at a time is also exactly how MEDIA-01 ("don't read the whole file into
/// memory") is satisfied by the higher-level <see cref="MediaClient.UploadFromStreamAsync"/> -
/// only ever one segment resident, never the whole asset.
/// </summary>
public sealed class AppendMediaUploadRequest
{
    public required string Id { get; init; }

    /// <summary>0-999, per the registry.</summary>
    public required int SegmentIndex { get; init; }

    public required byte[] Segment { get; init; }
}

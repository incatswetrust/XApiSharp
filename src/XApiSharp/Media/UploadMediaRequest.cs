using XApiSharp.Common;

namespace XApiSharp.Media;

/// <summary>
/// Low-level request for <c>POST /2/media/upload</c> (direct, non-chunked upload) - one HTTP
/// call, the whole asset. For anything large enough to want progress/deadline/resumability, use
/// the chunked sequence via <see cref="MediaClient.UploadFromStreamAsync"/> instead.
/// </summary>
public sealed class UploadMediaRequest
{
    /// <summary>Caller-owned (MEDIA-02) - not closed by the SDK. Read forward-only; a
    /// non-seekable stream is fine here since there's no retry-by-reread requirement for this
    /// single-call path beyond what <see cref="System.Net.Http.StreamContent"/> itself does.</summary>
    public required Stream Media { get; init; }

    public required XMediaCategory MediaCategory { get; init; }

    public IReadOnlyCollection<string>? AdditionalOwners { get; init; }

    /// <summary>MEDIA-06: reports cumulative bytes sent so far. A throwing callback is not
    /// swallowed - it propagates out of the upload call.</summary>
    public IProgress<long>? Progress { get; init; }
}

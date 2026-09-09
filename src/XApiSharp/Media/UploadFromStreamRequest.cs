using XApiSharp.Common;

namespace XApiSharp.Media;

/// <summary>
/// Request for <c>MediaClient.UploadFromStreamAsync</c> - the high-level "Stream → media ID"
/// facade (spec section 15.1) driving initialize → append* → finalize → wait-for-processing as
/// one call.
/// </summary>
public sealed class UploadFromStreamRequest
{
    /// <summary>Caller-owned (MEDIA-02) - never closed by the SDK, regardless of how the upload
    /// ends (success, failure, or cancellation).</summary>
    public required Stream Media { get; init; }

    public required XMediaCategory MediaCategory { get; init; }

    public required XMediaMimeType MediaType { get; init; }

    /// <summary>
    /// Required when <see cref="Media"/> is not seekable (MEDIA-03: the known-length requirement
    /// is explained up front, not discovered as a late server error) - the chunked-upload
    /// protocol needs the total size at <c>initialize</c> time, before any bytes are read.
    /// When <see cref="Media"/> is seekable and this is left unset, <c>Media.Length</c> is used.
    /// </summary>
    public long? TotalBytes { get; init; }

    public IReadOnlyCollection<string>? AdditionalOwners { get; init; }

    public bool? Shared { get; init; }

    /// <summary>MEDIA-06: reports cumulative bytes sent across every segment (not reset per
    /// segment). A throwing callback propagates rather than being swallowed.</summary>
    public IProgress<long>? Progress { get; init; }

    /// <summary>
    /// MEDIA-04: segment size is supposed to come from the current contract; the OpenAPI snapshot
    /// doesn't declare an explicit per-segment byte limit (only that <c>segment_index</c> tops
    /// out at 999), so this is a documented, overridable default (5 MiB) rather than a value
    /// pulled from the registry - check X's current chunked-upload guidance for your media type
    /// and override if it specifies something different.
    /// </summary>
    public int ChunkSizeBytes { get; init; } = 5 * 1024 * 1024;

    /// <summary>
    /// MEDIA-08: a separate, bounded deadline for the post-finalize processing-status poll loop -
    /// distinct from <see cref="XClientOptions.OperationTimeout"/>, which only covers a single
    /// HTTP call. Defaults to 5 minutes.
    /// </summary>
    public TimeSpan ProcessingTimeout { get; init; } = TimeSpan.FromMinutes(5);
}

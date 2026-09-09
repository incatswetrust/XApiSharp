namespace XApiSharp.Errors;

/// <summary>
/// Thrown by <c>MediaClient.UploadFromStreamAsync</c> when the high-level upload sequence
/// (initialize → append* → finalize → wait for processing) fails partway through (MEDIA-09: the
/// media ID and last-known processing state are preserved on the error rather than lost, so the
/// caller can inspect or resume via <c>GetUploadStatusAsync</c>/<c>FinalizeUploadAsync</c> using
/// <see cref="MediaId"/> - the SDK does not invent a cleanup/delete call itself (MEDIA-11), since
/// the registry doesn't declare one).
/// </summary>
public sealed class XMediaUploadException : XApiException
{
    /// <summary>The upload session id from <c>initialize</c> - always known once that step
    /// succeeds, <see langword="null"/> only if initialize itself failed.</summary>
    public string? MediaId { get; }

    public string? MediaKey { get; }

    /// <summary>Last known processing state (pending/in_progress/failed/succeeded) when this was
    /// thrown, if the upload got as far as finalize.</summary>
    public string? LastKnownState { get; }

    public XMediaUploadException(string message, string? mediaId, string? mediaKey, string? lastKnownState, Exception? innerException = null)
        : base(message, innerException: innerException)
    {
        MediaId = mediaId;
        MediaKey = mediaKey;
        LastKnownState = lastKnownState;
    }
}

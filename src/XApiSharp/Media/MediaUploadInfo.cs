using System.Text.Json.Serialization;

namespace XApiSharp.Media;

/// <summary>
/// Shared shape for "GetMediaUploadStatusResponseData"/"FinalizeMediaUploadResponseData"/
/// "MediaUploadResponseData" (identical in the registry) and a superset of
/// "InitializeMediaUploadResponseData" (which only ever populates <see cref="Id"/>/
/// <see cref="MediaKey"/>/<see cref="ExpiresAfterSecs"/> - every other field is naturally absent
/// this early, not a schema mismatch).
/// </summary>
public sealed class MediaUploadInfo
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("media_key")]
    public string? MediaKey { get; init; }

    [JsonPropertyName("size")]
    public long? Size { get; init; }

    [JsonPropertyName("expires_after_secs")]
    public int? ExpiresAfterSecs { get; init; }

    [JsonPropertyName("processing_info")]
    public MediaProcessingInfo? ProcessingInfo { get; init; }

    [JsonPropertyName("image")]
    public MediaUploadImageInfo? Image { get; init; }

    [JsonPropertyName("video")]
    public MediaUploadVideoInfo? Video { get; init; }
}

/// <summary>Central to MEDIA-08 (bounded processing-wait deadline) - <see cref="CheckAfterSecs"/>
/// is the server's own polling-interval hint.</summary>
public sealed class MediaProcessingInfo
{
    /// <summary>pending, in_progress, failed, succeeded, per the registry. Kept as a plain
    /// string (SER-06) rather than a closed enum - a response value the server could extend.</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("check_after_secs")]
    public int? CheckAfterSecs { get; init; }

    [JsonPropertyName("progress_percent")]
    public int? ProgressPercent { get; init; }
}

public sealed class MediaUploadImageInfo
{
    [JsonPropertyName("image_type")]
    public string? ImageType { get; init; }

    [JsonPropertyName("w")]
    public int? Width { get; init; }

    [JsonPropertyName("h")]
    public int? Height { get; init; }
}

public sealed class MediaUploadVideoInfo
{
    [JsonPropertyName("video_type")]
    public string? VideoType { get; init; }
}

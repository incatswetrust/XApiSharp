namespace XApiSharp.Common;

/// <summary>The <c>media_type</c> value on <c>POST /2/media/upload/initialize</c> - the actual
/// MIME type of the uploaded asset (not the HTTP request's own content type).</summary>
public enum XMediaMimeType
{
    VideoMp4,
    VideoWebm,
    VideoMp2t,
    VideoQuicktime,
    TextSrt,
    TextVtt,
    ImageJpeg,
    ImageGif,
    ImageBmp,
    ImagePng,
    ImageWebp,
    ImagePjpeg,
    ImageTiff,
    ModelGltfBinary,
    ModelVndUsdzZip,
}

public static class XMediaMimeTypeExtensions
{
    public static string ToApiValue(this XMediaMimeType value) => value switch
    {
        XMediaMimeType.VideoMp4 => "video/mp4",
        XMediaMimeType.VideoWebm => "video/webm",
        XMediaMimeType.VideoMp2t => "video/mp2t",
        XMediaMimeType.VideoQuicktime => "video/quicktime",
        XMediaMimeType.TextSrt => "text/srt",
        XMediaMimeType.TextVtt => "text/vtt",
        XMediaMimeType.ImageJpeg => "image/jpeg",
        XMediaMimeType.ImageGif => "image/gif",
        XMediaMimeType.ImageBmp => "image/bmp",
        XMediaMimeType.ImagePng => "image/png",
        XMediaMimeType.ImageWebp => "image/webp",
        XMediaMimeType.ImagePjpeg => "image/pjpeg",
        XMediaMimeType.ImageTiff => "image/tiff",
        XMediaMimeType.ModelGltfBinary => "model/gltf-binary",
        XMediaMimeType.ModelVndUsdzZip => "model/vnd.usdz+zip",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

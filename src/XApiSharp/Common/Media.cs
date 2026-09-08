using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>
/// Modeled from the "Media" schema - the shape of a media object as embedded in
/// <see cref="XIncludes.Media"/> via the <c>attachments.media_keys</c> expansion. Upload,
/// chunked-transfer, and other Media-family operations land in E5 (spec section 15); this is only
/// the read-side shape needed wherever a Post/DM/etc. response links to media.
/// </summary>
public sealed class Media
{
    [JsonPropertyName("media_key")]
    public required string MediaKey { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("preview_image_url")]
    public string? PreviewImageUrl { get; init; }

    [JsonPropertyName("alt_text")]
    public string? AltText { get; init; }

    [JsonPropertyName("width")]
    public int? Width { get; init; }

    [JsonPropertyName("height")]
    public int? Height { get; init; }

    [JsonPropertyName("duration_ms")]
    public int? DurationMs { get; init; }

    [JsonPropertyName("public_metrics")]
    public MediaPublicMetrics? PublicMetrics { get; init; }

    [JsonPropertyName("variants")]
    public IReadOnlyList<MediaVariant>? Variants { get; init; }

    private Dictionary<string, System.Text.Json.JsonElement>? _extensionData;

    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement> ExtensionData
    {
        get => _extensionData ??= [];
        set => _extensionData = value;
    }
}

public sealed class MediaPublicMetrics
{
    [JsonPropertyName("view_count")]
    public long ViewCount { get; init; }
}

public sealed class MediaVariant
{
    [JsonPropertyName("bit_rate")]
    public long? BitRate { get; init; }

    [JsonPropertyName("content_type")]
    public string? ContentType { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }
}

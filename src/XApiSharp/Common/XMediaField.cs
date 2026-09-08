namespace XApiSharp.Common;

/// <summary>The <c>media.fields</c> query parameter (SER-05) - selects which optional
/// <see cref="Media"/> fields to include in the response.</summary>
public enum XMediaField
{
    AltText,
    DurationMs,
    Height,
    MediaKey,
    NonPublicMetrics,
    OrganicMetrics,
    PreviewImageUrl,
    PromotedMetrics,
    PublicMetrics,
    Type,
    Url,
    Variants,
    Width,
}

public static class XMediaFieldExtensions
{
    public static string ToApiValue(this XMediaField field) => field switch
    {
        XMediaField.AltText => "alt_text",
        XMediaField.DurationMs => "duration_ms",
        XMediaField.Height => "height",
        XMediaField.MediaKey => "media_key",
        XMediaField.NonPublicMetrics => "non_public_metrics",
        XMediaField.OrganicMetrics => "organic_metrics",
        XMediaField.PreviewImageUrl => "preview_image_url",
        XMediaField.PromotedMetrics => "promoted_metrics",
        XMediaField.PublicMetrics => "public_metrics",
        XMediaField.Type => "type",
        XMediaField.Url => "url",
        XMediaField.Variants => "variants",
        XMediaField.Width => "width",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

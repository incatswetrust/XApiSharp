namespace XApiSharp.Common;

/// <summary>The <c>media_category</c> value on <c>POST /2/media/subtitles</c> specifically - a
/// different, narrower, PascalCase-valued enum than <see cref="XMediaCategory"/> (used by the
/// upload operations' own <c>media_category</c> field, which is snake_case). Same field name,
/// genuinely different contract per operation (PAGE-02).</summary>
public enum XSubtitlesMediaCategory
{
    AmplifyVideo,
    TweetVideo,
}

public static class XSubtitlesMediaCategoryExtensions
{
    public static string ToApiValue(this XSubtitlesMediaCategory value) => value switch
    {
        XSubtitlesMediaCategory.AmplifyVideo => "AmplifyVideo",
        XSubtitlesMediaCategory.TweetVideo => "TweetVideo",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

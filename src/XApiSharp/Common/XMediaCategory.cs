namespace XApiSharp.Common;

/// <summary>The <c>media_category</c> value on the media upload operations (direct upload and
/// chunked initialize) - the union of both operations' declared value sets in the registry
/// (initialize additionally allows <c>amplify_video</c>; not enforced per-operation client-side,
/// per API-10). Not the same value set as <c>CreateMediaSubtitlesRequest</c>'s own
/// <c>media_category</c> (see <see cref="XSubtitlesMediaCategory"/>), which uses different
/// (PascalCase) values entirely - PAGE-02's "don't assume all schemas are the same" applies to
/// this field name across operations.</summary>
public enum XMediaCategory
{
    AmplifyVideo,
    TweetGif,
    TweetImage,
    TweetVideo,
    DmGif,
    DmImage,
    DmVideo,
    Subtitles,
}

public static class XMediaCategoryExtensions
{
    public static string ToApiValue(this XMediaCategory value) => value switch
    {
        XMediaCategory.AmplifyVideo => "amplify_video",
        XMediaCategory.TweetGif => "tweet_gif",
        XMediaCategory.TweetImage => "tweet_image",
        XMediaCategory.TweetVideo => "tweet_video",
        XMediaCategory.DmGif => "dm_gif",
        XMediaCategory.DmImage => "dm_image",
        XMediaCategory.DmVideo => "dm_video",
        XMediaCategory.Subtitles => "subtitles",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

namespace XApiSharp.Common;

/// <summary>The <c>like_with_tweet_author.fields</c> query parameter - unique to the two Likes
/// streaming operations, selecting which fields of the Like-plus-author event to include.</summary>
public enum XLikeWithPostAuthorField
{
    AttachmentsMediaKeys,
    CreatedAt,
    Id,
    LikedTweetAuthorId,
    LikedTweetId,
    TimestampMs,
}

public static class XLikeWithPostAuthorFieldExtensions
{
    public static string ToApiValue(this XLikeWithPostAuthorField field) => field switch
    {
        XLikeWithPostAuthorField.AttachmentsMediaKeys => "attachments_media_keys",
        XLikeWithPostAuthorField.CreatedAt => "created_at",
        XLikeWithPostAuthorField.Id => "id",
        XLikeWithPostAuthorField.LikedTweetAuthorId => "liked_tweet_author_id",
        XLikeWithPostAuthorField.LikedTweetId => "liked_tweet_id",
        XLikeWithPostAuthorField.TimestampMs => "timestamp_ms",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

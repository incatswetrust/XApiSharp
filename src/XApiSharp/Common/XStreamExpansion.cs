namespace XApiSharp.Common;

/// <summary>
/// The streaming family's <c>expansions</c> query parameter (E5) - a differently-shaped, older
/// vocabulary than <see cref="XExpansion"/> (e.g. three granular
/// <c>referenced_tweets.id[.attachments.media_keys|.author_id]</c> values and
/// <c>entities.note.mentions.username</c> instead of the modern <c>referenced_posts</c>; no
/// <c>username</c> value at all). Union of every streaming operation's declared subset (Post
/// volume/filtered streams plus the two Likes streams' narrower <c>liked_tweet_id</c>/
/// <c>liked_tweet_author_id</c> set - API-10: not enforced per-operation client-side).
/// </summary>
public enum XStreamExpansion
{
    ArticleCoverMedia,
    ArticleMediaEntities,
    AttachmentsMediaKeys,
    AttachmentsMediaSourceTweet,
    AttachmentsPollIds,
    AuthorId,
    EditHistoryTweetIds,
    EntitiesMentionsUsername,
    EntitiesNoteMentionsUsername,
    GeoPlaceId,
    InReplyToUserId,
    LikedTweetAuthorId,
    LikedTweetId,
    ReferencedTweetsId,
    ReferencedTweetsIdAttachmentsMediaKeys,
    ReferencedTweetsIdAuthorId,
}

public static class XStreamExpansionExtensions
{
    public static string ToApiValue(this XStreamExpansion expansion) => expansion switch
    {
        XStreamExpansion.ArticleCoverMedia => "article.cover_media",
        XStreamExpansion.ArticleMediaEntities => "article.media_entities",
        XStreamExpansion.AttachmentsMediaKeys => "attachments.media_keys",
        XStreamExpansion.AttachmentsMediaSourceTweet => "attachments.media_source_tweet",
        XStreamExpansion.AttachmentsPollIds => "attachments.poll_ids",
        XStreamExpansion.AuthorId => "author_id",
        XStreamExpansion.EditHistoryTweetIds => "edit_history_tweet_ids",
        XStreamExpansion.EntitiesMentionsUsername => "entities.mentions.username",
        XStreamExpansion.EntitiesNoteMentionsUsername => "entities.note.mentions.username",
        XStreamExpansion.GeoPlaceId => "geo.place_id",
        XStreamExpansion.InReplyToUserId => "in_reply_to_user_id",
        XStreamExpansion.LikedTweetAuthorId => "liked_tweet_author_id",
        XStreamExpansion.LikedTweetId => "liked_tweet_id",
        XStreamExpansion.ReferencedTweetsId => "referenced_tweets.id",
        XStreamExpansion.ReferencedTweetsIdAttachmentsMediaKeys => "referenced_tweets.id.attachments.media_keys",
        XStreamExpansion.ReferencedTweetsIdAuthorId => "referenced_tweets.id.author_id",
        _ => throw new ArgumentOutOfRangeException(nameof(expansion), expansion, message: null),
    };
}

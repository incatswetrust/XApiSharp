namespace XApiSharp.Common;

/// <summary>
/// The <c>expansions</c> query parameter (SER-05) - requests a related object be embedded in the
/// response's <see cref="XIncludes"/> instead of only its bare ID. The OpenAPI snapshot declares a
/// different allowed subset per operation (9 distinct variants across the registry) rather than
/// one fixed list - per API-10, the SDK does not attempt to locally enforce which subset a given
/// operation accepts; an expansion the operation doesn't support is a server-side validation
/// error (surfaces as <see cref="Errors.XApiException"/>), not a client-side one. This is the
/// union of every value the snapshot declares anywhere in scope.
/// </summary>
public enum XExpansion
{
    AdminIds,
    Affiliation,
    ArticleCoverMedia,
    ArticleMediaEntities,
    AttachmentsMediaKeys,
    AttachmentsMediaSourceTweet,
    AttachmentsPollIds,
    AuthorId,
    CreatorId,
    EditHistoryPostIds,
    GeoPlaceId,
    HostIds,
    InReplyToUserId,
    InvitedUserIds,
    LikedTweetAuthorId,
    LikedTweetId,
    MemberIds,
    MostRecentPostId,
    OwnerId,
    ParticipantIds,
    PinnedPostId,
    ReferencedPosts,
    SenderId,
    SpeakerIds,
    TopicIds,
    Username,
    EntitiesMentionsUsername,
    EntitiesNoteMentionsUsername,
}

public static class XExpansionExtensions
{
    public static string ToApiValue(this XExpansion expansion) => expansion switch
    {
        XExpansion.AdminIds => "admin_ids",
        XExpansion.Affiliation => "affiliation",
        XExpansion.ArticleCoverMedia => "article.cover_media",
        XExpansion.ArticleMediaEntities => "article.media_entities",
        XExpansion.AttachmentsMediaKeys => "attachments.media_keys",
        XExpansion.AttachmentsMediaSourceTweet => "attachments.media_source_tweet",
        XExpansion.AttachmentsPollIds => "attachments.poll_ids",
        XExpansion.AuthorId => "author_id",
        XExpansion.CreatorId => "creator_id",
        XExpansion.EditHistoryPostIds => "edit_history_post_ids",
        XExpansion.GeoPlaceId => "geo.place_id",
        XExpansion.HostIds => "host_ids",
        XExpansion.InReplyToUserId => "in_reply_to_user_id",
        XExpansion.InvitedUserIds => "invited_user_ids",
        XExpansion.LikedTweetAuthorId => "liked_tweet_author_id",
        XExpansion.LikedTweetId => "liked_tweet_id",
        XExpansion.MemberIds => "member_ids",
        XExpansion.MostRecentPostId => "most_recent_post_id",
        XExpansion.OwnerId => "owner_id",
        XExpansion.ParticipantIds => "participant_ids",
        XExpansion.PinnedPostId => "pinned_post_id",
        XExpansion.ReferencedPosts => "referenced_posts",
        XExpansion.SenderId => "sender_id",
        XExpansion.SpeakerIds => "speaker_ids",
        XExpansion.TopicIds => "topic_ids",
        XExpansion.Username => "username",
        XExpansion.EntitiesMentionsUsername => "entities.mentions.username",
        XExpansion.EntitiesNoteMentionsUsername => "entities.note.mentions.username",
        _ => throw new ArgumentOutOfRangeException(nameof(expansion), expansion, message: null),
    };
}

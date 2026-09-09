namespace XApiSharp.Common;

/// <summary>
/// The <c>event_type</c> value on <c>createActivitySubscription</c> - a 42-value closed set per
/// the registry, in dot notation. Subscription-request-side only; <c>ActivityStreamEvent.EventType</c>
/// (<c>XApiSharp.Streaming</c>) deliberately stays a plain string (SER-06) rather than reusing this
/// enum - a value the server adds to the stream in the future must not become a breaking
/// deserialization failure for already-installed SDK versions, the same reasoning already applied
/// to <c>ComplianceJob.Status</c> elsewhere in this codebase.
/// </summary>
public enum XActivityEventType
{
    ProfileUpdateBio,
    ProfileUpdateProfilePicture,
    ProfileUpdateBannerPicture,
    ProfileUpdateScreenname,
    ProfileUpdateGeo,
    ProfileUpdateUrl,
    ProfileUpdateVerifiedBadge,
    ProfileUpdateAffiliateBadge,
    ProfileUpdateHandle,
    NewsNew,
    FollowFollow,
    FollowUnfollow,
    SpacesStart,
    SpacesEnd,
    BroadcastStart,
    BroadcastEnd,
    BroadcastChat,
    ChatReceived,
    ChatSent,
    ChatConversationJoin,

    /// <summary>The registry declares both <c>chat.conversation.join</c> (see
    /// <see cref="ChatConversationJoin"/>) and this <c>chat.conversation_join</c> value side by
    /// side in the same enum - looks like an upstream duplicate/typo, but both are implemented
    /// literally per the contract (API-10) rather than the SDK silently dropping one.</summary>
    ChatConversationJoinUnderscore,
    ChatConversationMemberAdded,
    ChatConversationMemberRemoved,
    ChatConversationAdminAdded,
    ChatConversationAdminRemoved,
    ChatUpdateGroupName,
    ChatUpdateRestrictions,
    DmSent,
    DmReceived,
    DmIndicateTyping,
    DmRead,
    PostCreate,
    PostDelete,
    PostMentionCreate,
    PostReplyCreate,
    PostQuoteCreate,
    PostRepostCreate,
    LikeCreate,
    MuteMute,
    MuteUnmute,
    BlockBlock,
    BlockUnblock,
}

public static class XActivityEventTypeExtensions
{
    public static string ToApiValue(this XActivityEventType value) => value switch
    {
        XActivityEventType.ProfileUpdateBio => "profile.update.bio",
        XActivityEventType.ProfileUpdateProfilePicture => "profile.update.profile_picture",
        XActivityEventType.ProfileUpdateBannerPicture => "profile.update.banner_picture",
        XActivityEventType.ProfileUpdateScreenname => "profile.update.screenname",
        XActivityEventType.ProfileUpdateGeo => "profile.update.geo",
        XActivityEventType.ProfileUpdateUrl => "profile.update.url",
        XActivityEventType.ProfileUpdateVerifiedBadge => "profile.update.verified_badge",
        XActivityEventType.ProfileUpdateAffiliateBadge => "profile.update.affiliate_badge",
        XActivityEventType.ProfileUpdateHandle => "profile.update.handle",
        XActivityEventType.NewsNew => "news.new",
        XActivityEventType.FollowFollow => "follow.follow",
        XActivityEventType.FollowUnfollow => "follow.unfollow",
        XActivityEventType.SpacesStart => "spaces.start",
        XActivityEventType.SpacesEnd => "spaces.end",
        XActivityEventType.BroadcastStart => "broadcast.start",
        XActivityEventType.BroadcastEnd => "broadcast.end",
        XActivityEventType.BroadcastChat => "broadcast.chat",
        XActivityEventType.ChatReceived => "chat.received",
        XActivityEventType.ChatSent => "chat.sent",
        XActivityEventType.ChatConversationJoin => "chat.conversation.join",
        XActivityEventType.ChatConversationJoinUnderscore => "chat.conversation_join",
        XActivityEventType.ChatConversationMemberAdded => "chat.conversation.member_added",
        XActivityEventType.ChatConversationMemberRemoved => "chat.conversation.member_removed",
        XActivityEventType.ChatConversationAdminAdded => "chat.conversation.admin_added",
        XActivityEventType.ChatConversationAdminRemoved => "chat.conversation.admin_removed",
        XActivityEventType.ChatUpdateGroupName => "chat.update.group_name",
        XActivityEventType.ChatUpdateRestrictions => "chat.update.restrictions",
        XActivityEventType.DmSent => "dm.sent",
        XActivityEventType.DmReceived => "dm.received",
        XActivityEventType.DmIndicateTyping => "dm.indicate_typing",
        XActivityEventType.DmRead => "dm.read",
        XActivityEventType.PostCreate => "post.create",
        XActivityEventType.PostDelete => "post.delete",
        XActivityEventType.PostMentionCreate => "post.mention.create",
        XActivityEventType.PostReplyCreate => "post.reply.create",
        XActivityEventType.PostQuoteCreate => "post.quote.create",
        XActivityEventType.PostRepostCreate => "post.repost.create",
        XActivityEventType.LikeCreate => "like.create",
        XActivityEventType.MuteMute => "mute.mute",
        XActivityEventType.MuteUnmute => "mute.unmute",
        XActivityEventType.BlockBlock => "block.block",
        XActivityEventType.BlockUnblock => "block.unblock",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

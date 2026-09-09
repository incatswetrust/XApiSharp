namespace XApiSharp.Common;

/// <summary>
/// The streaming family's <c>tweet.fields</c> query parameter (E5) - a differently-shaped, older
/// 34-value variant of <see cref="XPostField"/>'s 31-value <c>post.fields</c> set (e.g.
/// <c>note_tweet</c> instead of <c>note_post</c>, <c>article</c> instead of
/// <c>article_title</c>). PAGE-02's "don't assume all schemas are the same" applies to field
/// selectors, not just pagination tokens - every streaming GET operation in the registry that
/// selects Post fields uses this legacy parameter name and vocabulary, never
/// <see cref="XPostField"/>.
/// </summary>
public enum XStreamTweetField
{
    Article,
    Attachments,
    AuthorId,
    CardUri,
    CommunityId,
    ContextAnnotations,
    ConversationId,
    CreatedAt,
    DisplayTextRange,
    EditControls,
    EditHistoryTweetIds,
    Entities,
    Geo,
    Id,
    InReplyToUserId,
    Lang,
    MatchedMediaNotes,
    MediaMetadata,
    NonPublicMetrics,
    NoteRequestSuggestions,
    NoteTweet,
    OrganicMetrics,
    PaidPartnership,
    PossiblySensitive,
    PromotedMetrics,
    PublicMetrics,
    ReferencedTweets,
    ReplySettings,
    Scopes,
    Source,
    SuggestedSourceLinks,
    SuggestedSourceLinksWithCounts,
    Text,
    Withheld,
}

public static class XStreamTweetFieldExtensions
{
    public static string ToApiValue(this XStreamTweetField field) => field switch
    {
        XStreamTweetField.Article => "article",
        XStreamTweetField.Attachments => "attachments",
        XStreamTweetField.AuthorId => "author_id",
        XStreamTweetField.CardUri => "card_uri",
        XStreamTweetField.CommunityId => "community_id",
        XStreamTweetField.ContextAnnotations => "context_annotations",
        XStreamTweetField.ConversationId => "conversation_id",
        XStreamTweetField.CreatedAt => "created_at",
        XStreamTweetField.DisplayTextRange => "display_text_range",
        XStreamTweetField.EditControls => "edit_controls",
        XStreamTweetField.EditHistoryTweetIds => "edit_history_tweet_ids",
        XStreamTweetField.Entities => "entities",
        XStreamTweetField.Geo => "geo",
        XStreamTweetField.Id => "id",
        XStreamTweetField.InReplyToUserId => "in_reply_to_user_id",
        XStreamTweetField.Lang => "lang",
        XStreamTweetField.MatchedMediaNotes => "matched_media_notes",
        XStreamTweetField.MediaMetadata => "media_metadata",
        XStreamTweetField.NonPublicMetrics => "non_public_metrics",
        XStreamTweetField.NoteRequestSuggestions => "note_request_suggestions",
        XStreamTweetField.NoteTweet => "note_tweet",
        XStreamTweetField.OrganicMetrics => "organic_metrics",
        XStreamTweetField.PaidPartnership => "paid_partnership",
        XStreamTweetField.PossiblySensitive => "possibly_sensitive",
        XStreamTweetField.PromotedMetrics => "promoted_metrics",
        XStreamTweetField.PublicMetrics => "public_metrics",
        XStreamTweetField.ReferencedTweets => "referenced_tweets",
        XStreamTweetField.ReplySettings => "reply_settings",
        XStreamTweetField.Scopes => "scopes",
        XStreamTweetField.Source => "source",
        XStreamTweetField.SuggestedSourceLinks => "suggested_source_links",
        XStreamTweetField.SuggestedSourceLinksWithCounts => "suggested_source_links_with_counts",
        XStreamTweetField.Text => "text",
        XStreamTweetField.Withheld => "withheld",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

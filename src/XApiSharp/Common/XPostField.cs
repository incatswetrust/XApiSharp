namespace XApiSharp.Common;

/// <summary>
/// The <c>post.fields</c> query parameter (SER-05) - selects which optional <see cref="Post"/>
/// fields to include in the response. This is the parameter name every in-scope E4 operation
/// uses; the streaming family (E5) uses the differently-shaped legacy <c>tweet.fields</c>
/// parameter instead (PAGE-02 applies to field selectors too, not just pagination tokens) and is
/// out of scope until then.
/// </summary>
public enum XPostField
{
    Article,
    ArticleTitle,
    Attachments,
    CardUri,
    CommunityId,
    ContextAnnotations,
    ConversationId,
    CreatedAt,
    DisplayTextRange,
    EditControls,
    Entities,
    Geo,
    Id,
    Lang,
    MatchedMediaNotes,
    MediaMetadata,
    NonPublicMetrics,
    NotePost,
    NoteRequestSuggestions,
    OrganicMetrics,
    PaidPartnership,
    PossiblySensitive,
    PromotedMetrics,
    PublicMetrics,
    ReplySettings,
    Scopes,
    Source,
    SuggestedSourceLinks,
    SuggestedSourceLinksWithCounts,
    Text,
    Withheld,
}

public static class XPostFieldExtensions
{
    public static string ToApiValue(this XPostField field) => field switch
    {
        XPostField.Article => "article",
        XPostField.ArticleTitle => "article_title",
        XPostField.Attachments => "attachments",
        XPostField.CardUri => "card_uri",
        XPostField.CommunityId => "community_id",
        XPostField.ContextAnnotations => "context_annotations",
        XPostField.ConversationId => "conversation_id",
        XPostField.CreatedAt => "created_at",
        XPostField.DisplayTextRange => "display_text_range",
        XPostField.EditControls => "edit_controls",
        XPostField.Entities => "entities",
        XPostField.Geo => "geo",
        XPostField.Id => "id",
        XPostField.Lang => "lang",
        XPostField.MatchedMediaNotes => "matched_media_notes",
        XPostField.MediaMetadata => "media_metadata",
        XPostField.NonPublicMetrics => "non_public_metrics",
        XPostField.NotePost => "note_post",
        XPostField.NoteRequestSuggestions => "note_request_suggestions",
        XPostField.OrganicMetrics => "organic_metrics",
        XPostField.PaidPartnership => "paid_partnership",
        XPostField.PossiblySensitive => "possibly_sensitive",
        XPostField.PromotedMetrics => "promoted_metrics",
        XPostField.PublicMetrics => "public_metrics",
        XPostField.ReplySettings => "reply_settings",
        XPostField.Scopes => "scopes",
        XPostField.Source => "source",
        XPostField.SuggestedSourceLinks => "suggested_source_links",
        XPostField.SuggestedSourceLinksWithCounts => "suggested_source_links_with_counts",
        XPostField.Text => "text",
        XPostField.Withheld => "withheld",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

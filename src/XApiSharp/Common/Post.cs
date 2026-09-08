using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>
/// Modeled from the "Post" schema in the X API v2 OpenAPI snapshot. Follows the same typing
/// tradeoff as <see cref="XApiSharp.Users.User"/> (spec SER-09): scalars and the commonly-used
/// <see cref="PublicMetrics"/> are fully typed; richer nested shapes (<c>attachments</c>,
/// <c>entities</c>, <c>context_annotations</c>, <c>referenced_posts</c>, <c>edit_controls</c>,
/// <c>geo</c>, <c>scopes</c>, <c>note_post</c>, and similar) are <see cref="JsonElement"/> for now
/// - full typing lands as the families that most need a given nested shape are implemented.
/// </summary>
public sealed class Post
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("author_id")]
    public string? AuthorId { get; init; }

    [JsonPropertyName("in_reply_to_user_id")]
    public string? InReplyToUserId { get; init; }

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; init; }

    [JsonPropertyName("community_id")]
    public string? CommunityId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("lang")]
    public string? Lang { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("possibly_sensitive")]
    public bool? PossiblySensitive { get; init; }

    [JsonPropertyName("paid_partnership")]
    public bool? PaidPartnership { get; init; }

    [JsonPropertyName("reply_settings")]
    public string? ReplySettings { get; init; }

    [JsonPropertyName("card_uri")]
    public string? CardUri { get; init; }

    [JsonPropertyName("edit_history_post_ids")]
    public IReadOnlyList<string>? EditHistoryPostIds { get; init; }

    [JsonPropertyName("public_metrics")]
    public PostPublicMetrics? PublicMetrics { get; init; }

    [JsonPropertyName("attachments")]
    public JsonElement? Attachments { get; init; }

    [JsonPropertyName("context_annotations")]
    public JsonElement? ContextAnnotations { get; init; }

    [JsonPropertyName("display_text_range")]
    public JsonElement? DisplayTextRange { get; init; }

    [JsonPropertyName("edit_controls")]
    public JsonElement? EditControls { get; init; }

    [JsonPropertyName("entities")]
    public JsonElement? Entities { get; init; }

    [JsonPropertyName("geo")]
    public JsonElement? Geo { get; init; }

    [JsonPropertyName("matched_media_notes")]
    public JsonElement? MatchedMediaNotes { get; init; }

    [JsonPropertyName("media_metadata")]
    public JsonElement? MediaMetadata { get; init; }

    [JsonPropertyName("non_public_metrics")]
    public JsonElement? NonPublicMetrics { get; init; }

    [JsonPropertyName("note_post")]
    public JsonElement? NotePost { get; init; }

    [JsonPropertyName("note_request_suggestions")]
    public JsonElement? NoteRequestSuggestions { get; init; }

    [JsonPropertyName("organic_metrics")]
    public JsonElement? OrganicMetrics { get; init; }

    [JsonPropertyName("promoted_metrics")]
    public JsonElement? PromotedMetrics { get; init; }

    [JsonPropertyName("referenced_posts")]
    public JsonElement? ReferencedPosts { get; init; }

    [JsonPropertyName("scopes")]
    public JsonElement? Scopes { get; init; }

    [JsonPropertyName("suggested_source_links")]
    public JsonElement? SuggestedSourceLinks { get; init; }

    [JsonPropertyName("suggested_source_links_with_counts")]
    public JsonElement? SuggestedSourceLinksWithCounts { get; init; }

    [JsonPropertyName("article")]
    public JsonElement? Article { get; init; }

    [JsonPropertyName("article_title")]
    public JsonElement? ArticleTitle { get; init; }

    [JsonPropertyName("withheld")]
    public JsonElement? Withheld { get; init; }

    private Dictionary<string, JsonElement>? _extensionData;

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData
    {
        get => _extensionData ??= [];
        set => _extensionData = value;
    }
}

public sealed class PostPublicMetrics
{
    [JsonPropertyName("repost_count")]
    public long RepostCount { get; init; }

    [JsonPropertyName("reply_count")]
    public long ReplyCount { get; init; }

    [JsonPropertyName("like_count")]
    public long LikeCount { get; init; }

    [JsonPropertyName("quote_count")]
    public long QuoteCount { get; init; }

    [JsonPropertyName("bookmark_count")]
    public long BookmarkCount { get; init; }

    [JsonPropertyName("impression_count")]
    public long ImpressionCount { get; init; }
}

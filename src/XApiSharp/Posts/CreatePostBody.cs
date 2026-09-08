using System.Text.Json.Serialization;

namespace XApiSharp.Posts;

/// <summary>
/// Wire shape of <c>POST /2/tweets</c>'s JSON body. Kept separate from the public
/// <see cref="CreatePostRequest"/> so the public surface can use idiomatic C# types
/// (<see cref="Common.XReplySettings"/>, plain nullable properties) while the wire body applies
/// the registry's exact field names, and - per the schema note - always sends <c>text</c> (empty
/// string default) since the backend rejects an absent value even when only <c>media</c> is set.
/// </summary>
internal sealed class CreatePostBody
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = "";

    [JsonPropertyName("reply")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CreatePostReplyBody? Reply { get; init; }

    [JsonPropertyName("edit_options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CreatePostEditOptionsBody? EditOptions { get; init; }

    [JsonPropertyName("quote_tweet_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? QuoteTweetId { get; init; }

    [JsonPropertyName("media")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CreatePostMediaBody? Media { get; init; }

    [JsonPropertyName("poll")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CreatePostPollBody? Poll { get; init; }

    [JsonPropertyName("geo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CreatePostGeoBody? Geo { get; init; }

    [JsonPropertyName("reply_settings")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplySettings { get; init; }

    [JsonPropertyName("community_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CommunityId { get; init; }

    [JsonPropertyName("card_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CardUri { get; init; }

    [JsonPropertyName("direct_message_deep_link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DirectMessageDeepLink { get; init; }

    [JsonPropertyName("for_super_followers_only")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ForSuperFollowersOnly { get; init; }

    [JsonPropertyName("made_with_ai")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MadeWithAi { get; init; }

    [JsonPropertyName("nullcast")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Nullcast { get; init; }

    [JsonPropertyName("paid_partnership")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PaidPartnership { get; init; }

    [JsonPropertyName("share_with_followers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? ShareWithFollowers { get; init; }
}

internal sealed class CreatePostReplyBody
{
    [JsonPropertyName("in_reply_to_tweet_id")]
    public required string InReplyToTweetId { get; init; }

    [JsonPropertyName("exclude_reply_user_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? ExcludeReplyUserIds { get; init; }
}

internal sealed class CreatePostEditOptionsBody
{
    [JsonPropertyName("previous_post_id")]
    public required string PreviousPostId { get; init; }
}

internal sealed class CreatePostMediaBody
{
    [JsonPropertyName("media_ids")]
    public required IReadOnlyCollection<string> MediaIds { get; init; }

    [JsonPropertyName("tagged_user_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? TaggedUserIds { get; init; }

    [JsonPropertyName("preview_media_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PreviewMediaId { get; init; }

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("embeddable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Embeddable { get; init; }

    [JsonPropertyName("call_to_actions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public System.Text.Json.JsonElement? CallToActions { get; init; }
}

internal sealed class CreatePostPollBody
{
    [JsonPropertyName("options")]
    public required IReadOnlyCollection<string> Options { get; init; }

    [JsonPropertyName("duration_minutes")]
    public required int DurationMinutes { get; init; }
}

internal sealed class CreatePostGeoBody
{
    [JsonPropertyName("place_id")]
    public required string PlaceId { get; init; }
}

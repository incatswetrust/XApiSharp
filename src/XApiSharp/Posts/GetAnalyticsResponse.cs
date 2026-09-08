using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Modeled from the "GetPostsAnalyticsResponse" schema.</summary>
public sealed class GetAnalyticsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Analytics>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

/// <summary>Modeled from the "Analytics" schema. <c>timestamped_metrics</c> stays
/// <see cref="JsonElement"/> (SER-09) - a nested time-bucketed breakdown, low value to fully type
/// until a consumer needs it.</summary>
public sealed class Analytics
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("app_install_attempts")]
    public long? AppInstallAttempts { get; init; }

    [JsonPropertyName("app_opens")]
    public long? AppOpens { get; init; }

    [JsonPropertyName("bookmarks")]
    public long? Bookmarks { get; init; }

    [JsonPropertyName("detail_expands")]
    public long? DetailExpands { get; init; }

    [JsonPropertyName("email_tweet")]
    public long? EmailTweet { get; init; }

    [JsonPropertyName("engagements")]
    public long? Engagements { get; init; }

    [JsonPropertyName("follows")]
    public long? Follows { get; init; }

    [JsonPropertyName("hashtag_clicks")]
    public long? HashtagClicks { get; init; }

    [JsonPropertyName("impressions")]
    public long? Impressions { get; init; }

    [JsonPropertyName("likes")]
    public long? Likes { get; init; }

    [JsonPropertyName("media_views")]
    public long? MediaViews { get; init; }

    [JsonPropertyName("permalink_clicks")]
    public long? PermalinkClicks { get; init; }

    [JsonPropertyName("quote_tweets")]
    public long? QuoteTweets { get; init; }

    [JsonPropertyName("replies")]
    public long? Replies { get; init; }

    [JsonPropertyName("retweets")]
    public long? Retweets { get; init; }

    [JsonPropertyName("shares")]
    public long? Shares { get; init; }

    [JsonPropertyName("unfollows")]
    public long? Unfollows { get; init; }

    [JsonPropertyName("unlikes")]
    public long? Unlikes { get; init; }

    [JsonPropertyName("url_clicks")]
    public long? UrlClicks { get; init; }

    [JsonPropertyName("user_profile_clicks")]
    public long? UserProfileClicks { get; init; }

    [JsonPropertyName("timestamped_metrics")]
    public JsonElement? TimestampedMetrics { get; init; }
}

namespace XApiSharp.Common;

/// <summary>The <c>analytics.fields</c> query parameter (SER-05) - selects which optional
/// analytics fields to include in the response of <c>GET /2/tweets/analytics</c>.</summary>
public enum XAnalyticsField
{
    AppInstallAttempts,
    AppOpens,
    Bookmarks,
    DetailExpands,
    EmailTweet,
    Engagements,
    Follows,
    HashtagClicks,
    Id,
    Impressions,
    Likes,
    MediaViews,
    PermalinkClicks,
    QuoteTweets,
    Replies,
    Retweets,
    Shares,
    Timestamp,
    TimestampedMetrics,
    Unfollows,
    Unlikes,
    UrlClicks,
    UserProfileClicks,
}

public static class XAnalyticsFieldExtensions
{
    public static string ToApiValue(this XAnalyticsField field) => field switch
    {
        XAnalyticsField.AppInstallAttempts => "app_install_attempts",
        XAnalyticsField.AppOpens => "app_opens",
        XAnalyticsField.Bookmarks => "bookmarks",
        XAnalyticsField.DetailExpands => "detail_expands",
        XAnalyticsField.EmailTweet => "email_tweet",
        XAnalyticsField.Engagements => "engagements",
        XAnalyticsField.Follows => "follows",
        XAnalyticsField.HashtagClicks => "hashtag_clicks",
        XAnalyticsField.Id => "id",
        XAnalyticsField.Impressions => "impressions",
        XAnalyticsField.Likes => "likes",
        XAnalyticsField.MediaViews => "media_views",
        XAnalyticsField.PermalinkClicks => "permalink_clicks",
        XAnalyticsField.QuoteTweets => "quote_tweets",
        XAnalyticsField.Replies => "replies",
        XAnalyticsField.Retweets => "retweets",
        XAnalyticsField.Shares => "shares",
        XAnalyticsField.Timestamp => "timestamp",
        XAnalyticsField.TimestampedMetrics => "timestamped_metrics",
        XAnalyticsField.Unfollows => "unfollows",
        XAnalyticsField.Unlikes => "unlikes",
        XAnalyticsField.UrlClicks => "url_clicks",
        XAnalyticsField.UserProfileClicks => "user_profile_clicks",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

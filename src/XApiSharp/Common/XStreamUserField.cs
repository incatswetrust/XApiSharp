namespace XApiSharp.Common;

/// <summary>
/// The streaming family's <c>user.fields</c> query parameter (E5) - the older 26-value variant
/// flagged in <see cref="XUserField"/>'s own doc comment: a field-level <c>affiliation</c> plus
/// legacy <c>most_recent_tweet_id</c>/<c>pinned_tweet_id</c> names, and no
/// <c>subscriber_count</c>/<c>subscribes_to_you</c>.
/// </summary>
public enum XStreamUserField
{
    Affiliation,
    ConfirmedEmail,
    ConnectionStatus,
    CreatedAt,
    Description,
    Entities,
    Id,
    IsIdentityVerified,
    Location,
    MostRecentTweetId,
    Name,
    Parody,
    PinnedTweetId,
    ProfileBannerUrl,
    ProfileImageUrl,
    Protected,
    PublicMetrics,
    ReceivesYourDm,
    Subscription,
    SubscriptionType,
    Url,
    Username,
    Verified,
    VerifiedFollowersCount,
    VerifiedType,
    Withheld,
}

public static class XStreamUserFieldExtensions
{
    public static string ToApiValue(this XStreamUserField field) => field switch
    {
        XStreamUserField.Affiliation => "affiliation",
        XStreamUserField.ConfirmedEmail => "confirmed_email",
        XStreamUserField.ConnectionStatus => "connection_status",
        XStreamUserField.CreatedAt => "created_at",
        XStreamUserField.Description => "description",
        XStreamUserField.Entities => "entities",
        XStreamUserField.Id => "id",
        XStreamUserField.IsIdentityVerified => "is_identity_verified",
        XStreamUserField.Location => "location",
        XStreamUserField.MostRecentTweetId => "most_recent_tweet_id",
        XStreamUserField.Name => "name",
        XStreamUserField.Parody => "parody",
        XStreamUserField.PinnedTweetId => "pinned_tweet_id",
        XStreamUserField.ProfileBannerUrl => "profile_banner_url",
        XStreamUserField.ProfileImageUrl => "profile_image_url",
        XStreamUserField.Protected => "protected",
        XStreamUserField.PublicMetrics => "public_metrics",
        XStreamUserField.ReceivesYourDm => "receives_your_dm",
        XStreamUserField.Subscription => "subscription",
        XStreamUserField.SubscriptionType => "subscription_type",
        XStreamUserField.Url => "url",
        XStreamUserField.Username => "username",
        XStreamUserField.Verified => "verified",
        XStreamUserField.VerifiedFollowersCount => "verified_followers_count",
        XStreamUserField.VerifiedType => "verified_type",
        XStreamUserField.Withheld => "withheld",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

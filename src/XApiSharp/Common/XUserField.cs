namespace XApiSharp.Common;

/// <summary>
/// The <c>user.fields</c> query parameter (SER-05) - selects which optional
/// <see cref="XApiSharp.Users.User"/> fields to include in the response. Matches the 25-value set
/// the OpenAPI snapshot declares for
/// every in-scope E4 operation. The streaming family (E5) declares a different, older 28-value
/// variant with legacy field names (<c>most_recent_tweet_id</c>/<c>pinned_tweet_id</c>/a
/// field-level <c>affiliation</c> instead of the expansion) - PAGE-02's "don't assume all schemas
/// are the same" applies here too, so that variant gets its own type if/when E5 needs it, rather
/// than merging the two into one enum.
/// </summary>
public enum XUserField
{
    ConfirmedEmail,
    ConnectionStatus,
    CreatedAt,
    Description,
    Entities,
    Id,
    IsIdentityVerified,
    Location,
    Name,
    Parody,
    ProfileBannerUrl,
    ProfileImageUrl,
    Protected,
    PublicMetrics,
    ReceivesYourDm,
    SubscriberCount,
    SubscribesToYou,
    Subscription,
    SubscriptionType,
    Url,
    Username,
    Verified,
    VerifiedFollowersCount,
    VerifiedType,
    Withheld,
}

public static class XUserFieldExtensions
{
    public static string ToApiValue(this XUserField field) => field switch
    {
        XUserField.ConfirmedEmail => "confirmed_email",
        XUserField.ConnectionStatus => "connection_status",
        XUserField.CreatedAt => "created_at",
        XUserField.Description => "description",
        XUserField.Entities => "entities",
        XUserField.Id => "id",
        XUserField.IsIdentityVerified => "is_identity_verified",
        XUserField.Location => "location",
        XUserField.Name => "name",
        XUserField.Parody => "parody",
        XUserField.ProfileBannerUrl => "profile_banner_url",
        XUserField.ProfileImageUrl => "profile_image_url",
        XUserField.Protected => "protected",
        XUserField.PublicMetrics => "public_metrics",
        XUserField.ReceivesYourDm => "receives_your_dm",
        XUserField.SubscriberCount => "subscriber_count",
        XUserField.SubscribesToYou => "subscribes_to_you",
        XUserField.Subscription => "subscription",
        XUserField.SubscriptionType => "subscription_type",
        XUserField.Url => "url",
        XUserField.Username => "username",
        XUserField.Verified => "verified",
        XUserField.VerifiedFollowersCount => "verified_followers_count",
        XUserField.VerifiedType => "verified_type",
        XUserField.Withheld => "withheld",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

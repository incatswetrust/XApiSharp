using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Users;

/// <summary>
/// Modeled from the "User" schema in the X API v2 OpenAPI snapshot (spec section 9). Scalar and
/// commonly-used nested fields are fully typed; <c>entities</c>, <c>subscription</c>, and
/// <c>withheld</c> are deferred to <see cref="JsonElement"/> for now (SER-09: acceptable for
/// genuinely-not-yet-modeled parts) - full typing lands with the rest of the Users family in E4.
/// </summary>
public sealed class User
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("protected")]
    public bool? Protected { get; init; }

    [JsonPropertyName("verified")]
    public bool? Verified { get; init; }

    [JsonPropertyName("verified_type")]
    public string? VerifiedType { get; init; }

    [JsonPropertyName("verified_followers_count")]
    public int? VerifiedFollowersCount { get; init; }

    [JsonPropertyName("is_identity_verified")]
    public bool? IsIdentityVerified { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("profile_image_url")]
    public string? ProfileImageUrl { get; init; }

    [JsonPropertyName("profile_banner_url")]
    public string? ProfileBannerUrl { get; init; }

    [JsonPropertyName("pinned_post_id")]
    public string? PinnedPostId { get; init; }

    [JsonPropertyName("most_recent_post_id")]
    public string? MostRecentPostId { get; init; }

    [JsonPropertyName("parody")]
    public bool? Parody { get; init; }

    [JsonPropertyName("receives_your_dm")]
    public bool? ReceivesYourDm { get; init; }

    [JsonPropertyName("subscriber_count")]
    public int? SubscriberCount { get; init; }

    [JsonPropertyName("subscribes_to_you")]
    public bool? SubscribesToYou { get; init; }

    [JsonPropertyName("subscription_type")]
    public string? SubscriptionType { get; init; }

    [JsonPropertyName("confirmed_email")]
    public string? ConfirmedEmail { get; init; }

    [JsonPropertyName("public_metrics")]
    public UserPublicMetrics? PublicMetrics { get; init; }

    [JsonPropertyName("affiliation")]
    public UserAffiliation? Affiliation { get; init; }

    [JsonPropertyName("connection_status")]
    public IReadOnlyList<string>? ConnectionStatus { get; init; }

    [JsonPropertyName("entities")]
    public JsonElement? Entities { get; init; }

    [JsonPropertyName("subscription")]
    public JsonElement? Subscription { get; init; }

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

public sealed class UserPublicMetrics
{
    [JsonPropertyName("followers_count")]
    public int? FollowersCount { get; init; }

    [JsonPropertyName("following_count")]
    public int? FollowingCount { get; init; }

    [JsonPropertyName("like_count")]
    public int? LikeCount { get; init; }

    [JsonPropertyName("listed_count")]
    public int? ListedCount { get; init; }

    [JsonPropertyName("media_count")]
    public int? MediaCount { get; init; }

    [JsonPropertyName("post_count")]
    public int? PostCount { get; init; }
}

public sealed class UserAffiliation
{
    [JsonPropertyName("badge_url")]
    public string? BadgeUrl { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}

using XApiSharp.Common;

namespace XApiSharp.Posts;

/// <summary>
/// Request for <c>POST /2/tweets</c>, modeled from the "CreatePostsRequest" schema. <c>Text</c> is
/// required unless <c>Media</c> is set (the registry defaults the wire field to an empty string
/// so it's always sent - the backend rejects an absent value - handled internally, callers just
/// leave <see cref="Text"/> null when posting media-only).
/// </summary>
public sealed class CreatePostRequest
{
    public string? Text { get; init; }

    public CreatePostReply? Reply { get; init; }

    /// <summary>Set to edit an existing Post (<see cref="CreatePostEditOptions.PreviousPostId"/>)
    /// rather than create a new one.</summary>
    public CreatePostEditOptions? EditOptions { get; init; }

    public string? QuoteTweetId { get; init; }

    public CreatePostMedia? Media { get; init; }

    public CreatePostPoll? Poll { get; init; }

    public CreatePostGeo? Geo { get; init; }

    public XReplySettings? ReplySettings { get; init; }

    public string? CommunityId { get; init; }

    public string? CardUri { get; init; }

    public string? DirectMessageDeepLink { get; init; }

    public bool? ForSuperFollowersOnly { get; init; }

    public bool? MadeWithAi { get; init; }

    public bool? Nullcast { get; init; }

    public bool? PaidPartnership { get; init; }

    public bool? ShareWithFollowers { get; init; }
}

public sealed class CreatePostReply
{
    public required string InReplyToTweetId { get; init; }

    public IReadOnlyCollection<string>? ExcludeReplyUserIds { get; init; }
}

public sealed class CreatePostEditOptions
{
    public required string PreviousPostId { get; init; }
}

/// <summary>1-4 media IDs, per the registry.</summary>
public sealed class CreatePostMedia
{
    public required IReadOnlyCollection<string> MediaIds { get; init; }

    public IReadOnlyCollection<string>? TaggedUserIds { get; init; }

    public string? PreviewMediaId { get; init; }

    public string? Description { get; init; }

    public string? Title { get; init; }

    public bool? Embeddable { get; init; }

    /// <summary>Card call-to-action buttons (app_install/visit_site/watch_now) - kept as a raw
    /// JSON escape hatch (SER-09/SER-12) rather than three more nested types for a rarely-used
    /// marketing-card feature. Pass a <see cref="System.Text.Json.JsonElement"/> shaped like the
    /// registry's "CreatePostsMediaCallToActions" schema.</summary>
    public System.Text.Json.JsonElement? CallToActions { get; init; }
}

/// <summary>2-4 options (1-25 characters each), 5-10080 minute duration, per the registry.</summary>
public sealed class CreatePostPoll
{
    public required IReadOnlyCollection<string> Options { get; init; }

    public required int DurationMinutes { get; init; }
}

public sealed class CreatePostGeo
{
    public required string PlaceId { get; init; }
}

using System.Text.Json.Serialization;

namespace XApiSharp.Users;

/// <summary>Request for the batch form of <c>POST /2/users/{id}/bookmarks</c> - up to 25 Post IDs,
/// all placed in the same optional folder. See <see cref="CreateBookmarkRequest"/> for why this is
/// a separate type from the single-Post form.</summary>
public sealed class CreateBookmarksRequest
{
    public required string UserId { get; init; }

    public required IReadOnlyCollection<string> TweetIds { get; init; }

    public string? FolderId { get; init; }
}

/// <summary>Modeled from the batch branch of the "CreateUsersBookmarkResponse" schema - a
/// per-ID result list plus a separate per-ID failure list (spec section 12.1: partial success is
/// not silently dropped). <see cref="Errors"/> uses <see cref="BookmarkOperationError"/>, not the
/// shared <see cref="Errors.XProblem"/> Problem shape - the registry declares a different,
/// narrower shape here (<c>tweet_id</c>/<c>title</c>/<c>detail</c>, no <c>type</c>), so reusing
/// <c>XProblem</c> (which requires <c>Type</c>) would misrepresent the actual contract.</summary>
public sealed class CreateBookmarksResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<CreateBookmarksResultItem>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<BookmarkOperationError>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class CreateBookmarksResultItem
{
    [JsonPropertyName("tweet_id")]
    public required string TweetId { get; init; }

    [JsonPropertyName("bookmarked")]
    public bool Bookmarked { get; init; }
}

/// <summary>The batch-bookmark-specific per-ID failure shape - narrower than
/// <see cref="Errors.XProblem"/> (no <c>type</c>/<c>status</c>).</summary>
public sealed class BookmarkOperationError
{
    [JsonPropertyName("tweet_id")]
    public string? TweetId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
}

internal sealed class CreateBookmarksBody
{
    [JsonPropertyName("tweet_ids")]
    public required IReadOnlyCollection<string> TweetIds { get; init; }

    [JsonPropertyName("folder_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FolderId { get; init; }
}

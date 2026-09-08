using System.Text.Json.Serialization;

namespace XApiSharp.Users;

/// <summary>
/// Request for the single-Post form of <c>POST /2/users/{id}/bookmarks</c>. The registry's
/// "CreateUsersBookmarkRequest" schema accepts either one <c>tweet_id</c> or up to 25
/// <c>tweet_ids</c> (mutually exclusive) with a <c>oneOf</c> response shaped accordingly
/// (SER-07) - modeled here as two distinct request/response pairs
/// (<see cref="CreateBookmarkRequest"/>/<see cref="CreateBookmarkResponse"/> for one Post,
/// <see cref="CreateBookmarksRequest"/>/<see cref="CreateBookmarksResponse"/> for a batch) rather
/// than one type that tries to represent both shapes at once.
/// </summary>
public sealed class CreateBookmarkRequest
{
    public required string UserId { get; init; }

    public required string TweetId { get; init; }

    /// <summary>Optional Bookmark folder to add the Post to; omitted adds to the user's
    /// top-level Bookmarks.</summary>
    public string? FolderId { get; init; }
}

/// <summary>Modeled from the single-Post branch of the "CreateUsersBookmarkResponse" schema.
/// No documented error shape for this branch, so it doesn't implement
/// <see cref="IXErrorCarryingResponse"/> - the request either succeeds (this shape) or fails the
/// call outright (mapped to an <see cref="Errors.XApiException"/> by the transport).</summary>
public sealed class CreateBookmarkResponse
{
    [JsonPropertyName("data")]
    public CreateBookmarkResponseData? Data { get; init; }
}

public sealed class CreateBookmarkResponseData
{
    [JsonPropertyName("bookmarked")]
    public bool Bookmarked { get; init; }
}

internal sealed class CreateBookmarkBody
{
    [JsonPropertyName("tweet_id")]
    public required string TweetId { get; init; }

    // Omitted from the wire body entirely when not set - sending an explicit `folder_id: null`
    // is a different, undocumented shape the registry doesn't describe.
    [JsonPropertyName("folder_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FolderId { get; init; }
}

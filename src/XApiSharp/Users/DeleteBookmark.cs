using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{id}/bookmarks/{tweet_id}</c>.</summary>
public sealed class DeleteBookmarkRequest
{
    public required string UserId { get; init; }

    public required string TweetId { get; init; }
}

/// <summary>Modeled from the "DeleteUsersBookmarkResponse" schema.</summary>
public sealed class DeleteBookmarkResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteBookmarkResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteBookmarkResponseData
{
    [JsonPropertyName("bookmarked")]
    public bool Bookmarked { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/likes</c> - <see cref="UserId"/> likes
/// <see cref="TweetId"/>.</summary>
public sealed class LikePostRequest
{
    public required string UserId { get; init; }

    public required string TweetId { get; init; }
}

/// <summary>Modeled from the "LikePostResponse" schema.</summary>
public sealed class LikePostResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public LikePostResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class LikePostResponseData
{
    [JsonPropertyName("liked")]
    public bool Liked { get; init; }
}

internal sealed class LikePostBody
{
    [JsonPropertyName("tweet_id")]
    public required string TweetId { get; init; }
}

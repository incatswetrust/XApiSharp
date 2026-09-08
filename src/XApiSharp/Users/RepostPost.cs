using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/retweets</c> - <see cref="UserId"/> reposts
/// <see cref="TweetId"/>.</summary>
public sealed class RepostPostRequest
{
    public required string UserId { get; init; }

    public required string TweetId { get; init; }
}

/// <summary>Modeled from the "RepostPostResponse" schema.</summary>
public sealed class RepostPostResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public RepostPostResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class RepostPostResponseData
{
    [JsonPropertyName("retweeted")]
    public bool Retweeted { get; init; }

    [JsonPropertyName("rest_id")]
    public required string RestId { get; init; }
}

internal sealed class RepostPostBody
{
    [JsonPropertyName("tweet_id")]
    public required string TweetId { get; init; }
}

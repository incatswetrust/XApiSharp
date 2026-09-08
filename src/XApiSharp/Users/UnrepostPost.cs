using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{id}/retweets/{source_tweet_id}</c>.</summary>
public sealed class UnrepostPostRequest
{
    public required string UserId { get; init; }

    public required string SourceTweetId { get; init; }
}

/// <summary>Modeled from the "UnrepostPostResponse" schema.</summary>
public sealed class UnrepostPostResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnrepostPostResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnrepostPostResponseData
{
    [JsonPropertyName("retweeted")]
    public bool Retweeted { get; init; }
}

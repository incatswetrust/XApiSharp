using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>One NDJSON line shared by <c>streamLikesFirehose</c> and <c>streamLikesSample10</c> -
/// both declare the identical <c>{data: LikeWithPostAuthor, errors, includes}</c> shape.</summary>
public sealed class StreamLikeResponse
{
    [JsonPropertyName("data")]
    public LikeWithPostAuthor? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }
}

/// <summary>Modeled from the "LikeWithPostAuthor" schema - a Like event, with the liked Post's
/// author and the liked Post's id.</summary>
public sealed class LikeWithPostAuthor
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("liked_tweet_id")]
    public string? LikedTweetId { get; init; }

    [JsonPropertyName("tweet_author_id")]
    public string? TweetAuthorId { get; init; }

    [JsonPropertyName("timestamp_ms")]
    public int? TimestampMs { get; init; }
}

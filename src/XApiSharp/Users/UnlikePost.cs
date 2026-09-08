using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{id}/likes/{tweet_id}</c>.</summary>
public sealed class UnlikePostRequest
{
    public required string UserId { get; init; }

    public required string TweetId { get; init; }
}

/// <summary>Modeled from the "UnlikePostResponse" schema.</summary>
public sealed class UnlikePostResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnlikePostResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnlikePostResponseData
{
    [JsonPropertyName("liked")]
    public bool Liked { get; init; }
}

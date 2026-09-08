using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Request for <c>PUT /2/tweets/{tweet_id}/hidden</c>.</summary>
public sealed class HideReplyRequest
{
    public required string TweetId { get; init; }

    public required bool Hidden { get; init; }
}

/// <summary>Modeled from the "HidePostsReplyResponse" schema.</summary>
public sealed class HideReplyResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public HideReplyResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class HideReplyResponseData
{
    [JsonPropertyName("hidden")]
    public bool? Hidden { get; init; }
}

internal sealed class HideReplyBody
{
    [JsonPropertyName("hidden")]
    public required bool Hidden { get; init; }
}

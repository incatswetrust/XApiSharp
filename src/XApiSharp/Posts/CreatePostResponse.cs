using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Modeled from the "CreatePostsResponse" schema. Returned with HTTP 201.</summary>
public sealed class CreatePostResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreatePostResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreatePostResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("edit_history_post_ids")]
    public IReadOnlyList<string>? EditHistoryPostIds { get; init; }
}

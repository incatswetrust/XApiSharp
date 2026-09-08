using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Posts;

/// <summary>Request for <c>DELETE /2/tweets/{id}</c>.</summary>
public sealed class DeletePostRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "DeletePostsResponse" schema.</summary>
public sealed class DeletePostResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeletePostResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeletePostResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

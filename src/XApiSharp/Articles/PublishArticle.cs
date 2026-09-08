using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Articles;

/// <summary>Request for <c>POST /2/articles/{article_id}/publish</c>.</summary>
public sealed class PublishArticleRequest
{
    public required string ArticleId { get; init; }
}

/// <summary>Modeled from the "ArticlePublishResponse" schema.</summary>
public sealed class PublishArticleResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public PublishArticleResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class PublishArticleResponseData
{
    [JsonPropertyName("post_id")]
    public required string PostId { get; init; }
}

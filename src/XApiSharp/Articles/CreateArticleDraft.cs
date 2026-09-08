using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Articles;

/// <summary>
/// Request for <c>POST /2/articles/draft</c>. <see cref="ContentState"/> is a raw
/// <see cref="JsonElement"/> escape hatch (SER-09/SER-12) matching the registry's
/// "ArticleCreateDraftContentState" shape - a rich-text editor document (blocks + entities:
/// links, embedded posts, images, emoji, markdown/code/tables, dividers, LaTeX) too deep and
/// editor-specific to model faithfully as C# types without a concrete consumer driving the
/// need; pass a <see cref="JsonElement"/> built to match that schema.
/// </summary>
public sealed class CreateArticleDraftRequest
{
    public required string Title { get; init; }

    public required JsonElement ContentState { get; init; }

    public ArticleCoverMedia? CoverMedia { get; init; }
}

public sealed class ArticleCoverMedia
{
    public required string MediaId { get; init; }

    /// <summary>E.g. <c>tweet_image</c>, per the registry - no documented closed enum.</summary>
    public required string MediaCategory { get; init; }
}

/// <summary>Modeled from the "ArticleCreateDraftResponse" schema. Returned with HTTP 201.</summary>
public sealed class CreateArticleDraftResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateArticleDraftResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateArticleDraftResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }
}

internal sealed class CreateArticleDraftBody
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("content_state")]
    public required JsonElement ContentState { get; init; }

    [JsonPropertyName("cover_media")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ArticleCoverMediaBody? CoverMedia { get; init; }
}

internal sealed class ArticleCoverMediaBody
{
    [JsonPropertyName("media_id")]
    public required string MediaId { get; init; }

    [JsonPropertyName("media_category")]
    public required string MediaCategory { get; init; }
}

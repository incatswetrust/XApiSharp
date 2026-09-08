using XApiSharp.Transport;

namespace XApiSharp.Articles;

/// <summary>Typed methods for the Articles family (2 operations per the registry).</summary>
public sealed class ArticlesClient
{
    private readonly RequestExecutor _executor;

    internal ArticlesClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>POST /2/articles/draft</c> - Create draft Article. Requires OAuth 2.0
    /// <c>tweet.write</c> or OAuth 1.0a. Returns HTTP 201.
    /// </summary>
    public Task<XResponse<CreateArticleDraftResponse>> CreateDraftAsync(CreateArticleDraftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);

        var body = new CreateArticleDraftBody
        {
            Title = request.Title,
            ContentState = request.ContentState,
            CoverMedia = request.CoverMedia is { } cover
                ? new ArticleCoverMediaBody { MediaId = cover.MediaId, MediaCategory = cover.MediaCategory }
                : null,
        };

        return _executor.SendAsync<CreateArticleDraftResponse>(HttpMethod.Post, "2/articles/draft", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/articles/{article_id}/publish</c> - Publish Article. Requires OAuth 2.0
    /// <c>tweet.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<PublishArticleResponse>> PublishAsync(PublishArticleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ArticleId);

        return _executor.SendAsync<PublishArticleResponse>(
            HttpMethod.Post,
            $"2/articles/{Uri.EscapeDataString(request.ArticleId)}/publish",
            cancellationToken);
    }
}

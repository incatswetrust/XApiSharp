using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Media;

/// <summary>
/// Typed methods for the Media family (11 operations per the registry, spec section 15). This
/// covers the 9 plain-JSON operations; the 2 binary-body operations (direct upload, chunked
/// append) and the high-level <c>Stream → media ID</c> facade land in a follow-up commit that
/// also adds multipart/form-data transport support.
/// </summary>
public sealed class MediaClient
{
    private readonly RequestExecutor _executor;

    internal MediaClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/media/{media_key}</c> - Get Media by media key. Requires app-only bearer, OAuth
    /// 2.0 <c>tweet.read</c>, or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetMediaResponse>> GetByKeyAsync(GetMediaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaKey);

        var query = new List<(string Name, string? Value)>
        {
            ("media.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetMediaResponse>(
            HttpMethod.Get,
            $"2/media/{Uri.EscapeDataString(request.MediaKey)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/media</c> - Get Media by media keys (up to 100 per call). Requires app-only
    /// bearer, OAuth 2.0 <c>tweet.read</c>, or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetMediaByKeysResponse>> GetByKeysAsync(GetMediaByKeysRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MediaKeys.Count == 0)
        {
            throw new ArgumentException("At least one media key is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("media_keys", QueryStringBuilder.JoinCommaSeparated(request.MediaKeys)),
        };
        query.Add(("media.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())));

        return _executor.SendAsync<GetMediaByKeysResponse>(HttpMethod.Get, "2/media", query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/media/analytics</c> - Get Media analytics. Requires OAuth 2.0 <c>tweet.read</c>
    /// or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetMediaAnalyticsResponse>> GetAnalyticsAsync(GetMediaAnalyticsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MediaKeys.Count == 0)
        {
            throw new ArgumentException("At least one media key is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("media_keys", QueryStringBuilder.JoinCommaSeparated(request.MediaKeys)),
            ("start_time", request.StartTime.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime.ToString("O", CultureInfo.InvariantCulture)),
            ("granularity", request.Granularity?.ToApiValue()),
            ("media_analytics.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetMediaAnalyticsResponse>(HttpMethod.Get, "2/media/analytics", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/media/metadata</c> - Create Media metadata. Requires OAuth 2.0
    /// <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<CreateMediaMetadataResponse>> CreateMetadataAsync(CreateMediaMetadataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var body = new CreateMediaMetadataBody { Id = request.Id, Metadata = request.Metadata };

        return _executor.SendAsync<CreateMediaMetadataResponse>(HttpMethod.Post, "2/media/metadata", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/media/subtitles</c> - Delete Media subtitles. Requires OAuth 2.0
    /// <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<DeleteMediaSubtitlesResponse>> DeleteSubtitlesAsync(DeleteMediaSubtitlesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LanguageCode);

        var body = new DeleteMediaSubtitlesBody { Id = request.Id, MediaCategory = request.MediaCategory, LanguageCode = request.LanguageCode };

        return _executor.SendAsync<DeleteMediaSubtitlesResponse>(HttpMethod.Delete, "2/media/subtitles", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/media/subtitles</c> - Create Media subtitles. Requires OAuth 2.0
    /// <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<CreateMediaSubtitlesResponse>> CreateSubtitlesAsync(CreateMediaSubtitlesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        if (request.Subtitles.Count == 0)
        {
            throw new ArgumentException("At least one subtitle track is required.", nameof(request));
        }

        var body = new CreateMediaSubtitlesBody
        {
            Id = request.Id,
            MediaCategory = request.MediaCategory?.ToApiValue(),
            Subtitles = request.Subtitles
                .Select(s => new MediaSubtitleBody { Id = s.Id, LanguageCode = s.LanguageCode, DisplayName = s.DisplayName })
                .ToList(),
        };

        return _executor.SendAsync<CreateMediaSubtitlesResponse>(HttpMethod.Post, "2/media/subtitles", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/media/upload</c> - Get Media upload status. Requires OAuth 2.0
    /// <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetMediaUploadStatusResponse>> GetUploadStatusAsync(GetMediaUploadStatusRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaId);

        var query = new List<(string Name, string? Value)>
        {
            ("media_id", request.MediaId),
            ("command", "STATUS"),
        };

        return _executor.SendAsync<GetMediaUploadStatusResponse>(HttpMethod.Get, "2/media/upload", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/media/upload/initialize</c> - Initialize media upload (spec section 15.1, step
    /// 2). Requires OAuth 2.0 <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<InitializeMediaUploadResponse>> InitializeUploadAsync(InitializeMediaUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TotalBytes < 0)
        {
            throw new ArgumentException("TotalBytes cannot be negative.", nameof(request));
        }

        var body = new InitializeMediaUploadBody
        {
            MediaCategory = request.MediaCategory.ToApiValue(),
            MediaType = request.MediaType.ToApiValue(),
            TotalBytes = request.TotalBytes,
            AdditionalOwners = request.AdditionalOwners,
            Shared = request.Shared,
        };

        return _executor.SendAsync<InitializeMediaUploadResponse>(HttpMethod.Post, "2/media/upload/initialize", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/media/upload/{id}/finalize</c> - Finalize Media upload (spec section 15.1,
    /// step 4). Requires OAuth 2.0 <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<FinalizeMediaUploadResponse>> FinalizeUploadAsync(FinalizeMediaUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<FinalizeMediaUploadResponse>(
            HttpMethod.Post,
            $"2/media/upload/{Uri.EscapeDataString(request.Id)}/finalize",
            cancellationToken);
    }
}

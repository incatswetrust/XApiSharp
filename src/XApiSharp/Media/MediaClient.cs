using System.Globalization;
using System.Net.Http.Headers;
using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Transport;

namespace XApiSharp.Media;

/// <summary>Typed methods for the Media family (11 operations per the registry, spec section 15),
/// including the chunked-upload sequence and the high-level <c>Stream → media ID</c> facade.</summary>
public sealed class MediaClient
{
    private readonly RequestExecutor _executor;
    private readonly TimeProvider _timeProvider;

    internal MediaClient(RequestExecutor executor, TimeProvider timeProvider)
    {
        _executor = executor;
        _timeProvider = timeProvider;
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

    /// <summary>
    /// <c>POST /2/media/upload</c> - direct, non-chunked upload (one HTTP call, the whole asset).
    /// Requires OAuth 2.0 <c>media.write</c> or OAuth 1.0a. See <see cref="UploadMediaRequest"/>
    /// for why this differs from the chunked sequence.
    /// </summary>
    public Task<XResponse<UploadMediaResponse>> UploadAsync(UploadMediaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Media);

        return _executor.SendMultipartAsync<UploadMediaResponse>(
            HttpMethod.Post,
            "2/media/upload",
            () =>
            {
                var content = new MultipartFormDataContent();
                var mediaContent = new StreamContent(new ProgressReportingStream(request.Media, request.Progress));
                mediaContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(mediaContent, "media", "media");
                content.Add(new StringContent(request.MediaCategory.ToApiValue()), "media_category");
                if (request.AdditionalOwners is { Count: > 0 })
                {
                    content.Add(new StringContent(QueryStringBuilder.JoinCommaSeparated(request.AdditionalOwners)!), "additional_owners");
                }

                return content;
            },
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/media/upload/{id}/append</c> - append one bounded, already-buffered segment
    /// (spec section 15.1, step 3; MEDIA-05: the caller can retry this call with the exact same
    /// bytes since <see cref="AppendMediaUploadRequest.Segment"/> is a buffered <c>byte[]</c>).
    /// Requires OAuth 2.0 <c>media.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<AppendMediaUploadResponse>> AppendUploadAsync(AppendMediaUploadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentNullException.ThrowIfNull(request.Segment);
        if (request.SegmentIndex is < 0 or > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.SegmentIndex, "SegmentIndex must be 0-999.");
        }

        return _executor.SendMultipartAsync<AppendMediaUploadResponse>(
            HttpMethod.Post,
            $"2/media/upload/{Uri.EscapeDataString(request.Id)}/append",
            () =>
            {
                var content = new MultipartFormDataContent();
                var mediaContent = new ByteArrayContent(request.Segment);
                mediaContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(mediaContent, "media", "segment");
                content.Add(new StringContent(request.SegmentIndex.ToString(CultureInfo.InvariantCulture)), "segment_index");

                return content;
            },
            cancellationToken);
    }

    /// <summary>
    /// High-level "Stream → media ID" facade (spec section 15.1) driving initialize → append* →
    /// finalize → wait-for-processing as one call, satisfying MEDIA-01 through MEDIA-12. On
    /// failure partway through, throws <see cref="XMediaUploadException"/> carrying whatever
    /// media ID/key/processing state is already known (MEDIA-09) - the SDK never invents a
    /// cleanup/delete call of its own (MEDIA-11).
    /// </summary>
    public async Task<MediaUploadInfo> UploadFromStreamAsync(UploadFromStreamRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Media);
        if (request.ChunkSizeBytes <= 0)
        {
            throw new ArgumentException("ChunkSizeBytes must be positive.", nameof(request));
        }

        // MEDIA-03: the known-length requirement is explained up front, not discovered as a late
        // server error.
        var totalBytes = request.TotalBytes ?? (request.Media.CanSeek
            ? request.Media.Length
            : throw new ArgumentException(
                "TotalBytes must be supplied when Media is not seekable - the chunked-upload protocol needs the total size at initialize time, before any bytes are read.",
                nameof(request)));

        // Fail before any HTTP call, not after burning ~1000 real append requests: segment_index
        // tops out at 999 (AppendMediaUploadRequest.SegmentIndex), so ChunkSizeBytes must be large
        // enough to cover TotalBytes in at most 1000 segments.
        var requiredSegments = (totalBytes + request.ChunkSizeBytes - 1) / request.ChunkSizeBytes;
        if (requiredSegments > 1000)
        {
            throw new ArgumentException(
                $"TotalBytes ({totalBytes:N0}) would need {requiredSegments:N0} segments at ChunkSizeBytes={request.ChunkSizeBytes:N0}, exceeding the registry's 1000-segment limit (segment_index is 0-999) - increase ChunkSizeBytes.",
                nameof(request));
        }

        var initializeResponse = await InitializeUploadAsync(
            new InitializeMediaUploadRequest
            {
                MediaCategory = request.MediaCategory,
                MediaType = request.MediaType,
                TotalBytes = totalBytes,
                AdditionalOwners = request.AdditionalOwners,
                Shared = request.Shared,
            },
            cancellationToken).ConfigureAwait(false);

        var mediaId = initializeResponse.Body?.Data?.Id
            ?? throw new XMediaUploadException("Initialize media upload returned no media id.", mediaId: null, mediaKey: null, lastKnownState: null);
        var mediaKey = initializeResponse.Body?.Data?.MediaKey;

        try
        {
            // MEDIA-01: only ever one bounded segment resident in memory at a time, never the
            // whole asset.
            var buffer = new byte[request.ChunkSizeBytes];
            var segmentIndex = 0;
            long bytesSent = 0;

            while (true)
            {
                var segmentLength = await ReadFullSegmentAsync(request.Media, buffer, cancellationToken).ConfigureAwait(false);
                if (segmentLength == 0)
                {
                    break;
                }

                // Safe to hand out the same reused buffer array (no copy) for a full-length
                // segment: AppendUploadAsync's HttpContent is fully serialized out of it before
                // that await returns (standard HttpContent.CopyToAsync semantics - the send
                // completes, or throws, before this loop overwrites the array on the next
                // iteration), so no torn segment is possible. A future refactor toward
                // fire-and-forget or lazily-read content would break this assumption.
                var segment = segmentLength == buffer.Length ? buffer : buffer[..segmentLength];

                await AppendUploadAsync(
                    new AppendMediaUploadRequest { Id = mediaId, SegmentIndex = segmentIndex, Segment = segment },
                    cancellationToken).ConfigureAwait(false);

                bytesSent += segmentLength;
                // MEDIA-06: cumulative, not per-segment; a throwing callback propagates.
                request.Progress?.Report(bytesSent);

                if (segmentLength < buffer.Length)
                {
                    break;
                }

                segmentIndex++;
            }

            var finalizeResponse = await FinalizeUploadAsync(new FinalizeMediaUploadRequest { Id = mediaId }, cancellationToken).ConfigureAwait(false);
            var info = finalizeResponse.Body?.Data
                ?? throw new XMediaUploadException("Finalize media upload returned no data.", mediaId, mediaKey, lastKnownState: null);

            if (info.ProcessingInfo is null)
            {
                return info;
            }

            return await WaitForProcessingAsync(mediaId, mediaKey, info, request.ProcessingTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not XMediaUploadException)
        {
            throw new XMediaUploadException($"Media upload failed: {ex.Message}", mediaId, mediaKey, lastKnownState: null, innerException: ex);
        }
    }

    /// <summary>
    /// MEDIA-08: a bounded deadline for the processing-status poll loop, driven by
    /// <see cref="_timeProvider"/> so tests never need a real sleep, honoring the server's own
    /// <c>check_after_secs</c> hint for the wait between polls.
    /// </summary>
    private async Task<MediaUploadInfo> WaitForProcessingAsync(string mediaId, string? mediaKey, MediaUploadInfo initialInfo, TimeSpan processingTimeout, CancellationToken cancellationToken)
    {
        var info = initialInfo;
        var deadline = _timeProvider.GetUtcNow() + processingTimeout;

        while (true)
        {
            var state = info.ProcessingInfo?.State;
            if (state is "succeeded" or null)
            {
                return info;
            }

            if (state == "failed")
            {
                throw new XMediaUploadException("Media processing failed.", mediaId, mediaKey, lastKnownState: state);
            }

            var checkAfter = TimeSpan.FromSeconds(Math.Max(1, info.ProcessingInfo?.CheckAfterSecs ?? 1));
            if (_timeProvider.GetUtcNow() + checkAfter > deadline)
            {
                throw new XMediaUploadException("Timed out waiting for media processing to finish.", mediaId, mediaKey, lastKnownState: state);
            }

            await Task.Delay(checkAfter, _timeProvider, cancellationToken).ConfigureAwait(false);

            var statusResponse = await GetUploadStatusAsync(new GetMediaUploadStatusRequest { MediaId = mediaId }, cancellationToken).ConfigureAwait(false);
            info = statusResponse.Body?.Data
                ?? throw new XMediaUploadException("Get media upload status returned no data.", mediaId, mediaKey, lastKnownState: state);
        }
    }

    /// <summary>Reads until <paramref name="buffer"/> is full or the stream ends (a single
    /// <c>ReadAsync</c> call is not guaranteed to fill the buffer) - MEDIA-05 needs each segment
    /// sent to be exactly the bytes it claims, not a short read.</summary>
    private static async Task<int> ReadFullSegmentAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}

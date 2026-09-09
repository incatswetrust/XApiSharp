using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Media;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the 2 binary-body Media operations (direct upload, chunked append) and
/// the high-level <see cref="MediaClient.UploadFromStreamAsync"/> facade, per spec section 19.3 -
/// the follow-up to <see cref="MediaContractTests"/> that also exercises multipart/form-data
/// transport (MEDIA-01 through MEDIA-12). The two timing-dependent scenarios (poll-until-success,
/// deadline-exceeded) live in XApiSharp.UnitTests alongside the other FakeTimeProvider-driven
/// tests instead, since this project intentionally has no dependency on
/// Microsoft.Extensions.TimeProvider.Testing.
/// </summary>
public class MediaUploadContractTests
{
    [Fact]
    public async Task UploadAsync_sends_media_and_category_as_multipart_parts()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/media/upload", request.RequestUri!.AbsolutePath);
            var parts = await ReadMultipartPartsAsync(request.Content!, ct);
            Assert.Equal(new byte[] { 1, 2, 3 }, parts["media"]);
            Assert.Equal("tweet_image", Encoding.UTF8.GetString(parts["media_category"]));
            Assert.Equal("owner1,owner2", Encoding.UTF8.GetString(parts["additional_owners"]));
            return SuccessResponse("""{"data":{"id":"1","media_key":"3_1"}}""");
        });
        var client = CreateClient(handler);
        using var stream = new MemoryStream([1, 2, 3]);

        var response = await client.Media.UploadAsync(new UploadMediaRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetImage,
            AdditionalOwners = ["owner1", "owner2"],
        });

        Assert.Equal("3_1", response.Body!.Data!.MediaKey);
    }

    [Fact]
    public async Task AppendUploadAsync_substitutes_id_and_sends_segment_bytes_and_index()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/media/upload/1/append", request.RequestUri!.AbsolutePath);
            var parts = await ReadMultipartPartsAsync(request.Content!, ct);
            Assert.Equal(new byte[] { 9, 8, 7 }, parts["media"]);
            Assert.Equal("2", Encoding.UTF8.GetString(parts["segment_index"]));
            return SuccessResponse("""{"data":{"expires_at":123}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Media.AppendUploadAsync(new AppendMediaUploadRequest
        {
            Id = "1",
            SegmentIndex = 2,
            Segment = [9, 8, 7],
        });

        Assert.Equal(123, response.Body!.Data!.ExpiresAt);
    }

    [Fact]
    public async Task AppendUploadAsync_rejects_segment_index_out_of_range()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("should not be called"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.Media.AppendUploadAsync(new AppendMediaUploadRequest
        {
            Id = "1",
            SegmentIndex = 1000,
            Segment = [1],
        }));
    }

    [Fact]
    public async Task UploadFromStreamAsync_chunks_a_seekable_stream_and_finalizes_when_processing_is_not_required()
    {
        var appendedSegments = new List<(int Index, byte[] Bytes)>();
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/2/media/upload/initialize")
            {
                var parts = await ReadJsonAsync(request, ct);
                Assert.Equal(7, parts.GetProperty("total_bytes").GetInt64());
                return SuccessResponse("""{"data":{"id":"m1","media_key":"3_m1"}}""");
            }

            if (path == "/2/media/upload/m1/append")
            {
                var multipart = await ReadMultipartPartsAsync(request.Content!, ct);
                var index = int.Parse(Encoding.UTF8.GetString(multipart["segment_index"]), System.Globalization.CultureInfo.InvariantCulture);
                appendedSegments.Add((index, multipart["media"]));
                return SuccessResponse("""{"data":{}}""");
            }

            if (path == "/2/media/upload/m1/finalize")
            {
                return SuccessResponse("""{"data":{"id":"m1","media_key":"3_m1"}}""");
            }

            throw new InvalidOperationException($"Unexpected path: {path}");
        });
        var client = CreateClient(handler);
        using var stream = new MemoryStream([1, 2, 3, 4, 5, 6, 7]);
        var progressReports = new List<long>();

        var info = await client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            ChunkSizeBytes = 3,
            Progress = new Progress<long>(progressReports.Add),
        });

        Assert.Equal("m1", info.Id);
        Assert.Equal(3, appendedSegments.Count);
        Assert.Equal(new byte[] { 1, 2, 3 }, appendedSegments[0].Bytes);
        Assert.Equal(new byte[] { 4, 5, 6 }, appendedSegments[1].Bytes);
        Assert.Equal(new byte[] { 7 }, appendedSegments[2].Bytes);
        Assert.Equal([0, 1, 2], appendedSegments.Select(s => s.Index));
    }

    [Fact]
    public async Task UploadFromStreamAsync_requires_TotalBytes_when_the_stream_is_not_seekable()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("should not be called"));
        var client = CreateClient(handler);
        using var stream = new NonSeekableStream([1, 2, 3]);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
        }));
    }

    [Fact]
    public async Task UploadFromStreamAsync_throws_with_last_known_state_when_processing_fails()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/2/media/upload/initialize")
            {
                return Task.FromResult(SuccessResponse("""{"data":{"id":"m1","media_key":"3_m1"}}"""));
            }

            if (path == "/2/media/upload/m1/finalize")
            {
                return Task.FromResult(SuccessResponse("""{"data":{"id":"m1","processing_info":{"state":"failed","check_after_secs":1}}}"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {path}");
        });
        var client = CreateClient(handler);
        using var stream = new MemoryStream([]);

        var ex = await Assert.ThrowsAsync<XMediaUploadException>(() => client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            TotalBytes = 0,
        }));

        Assert.Equal("m1", ex.MediaId);
        Assert.Equal("3_m1", ex.MediaKey);
        Assert.Equal("failed", ex.LastKnownState);
    }

    [Fact]
    public async Task UploadFromStreamAsync_honors_an_already_cancelled_token_before_any_call()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("should not be called"));
        var client = CreateClient(handler);
        using var stream = new MemoryStream([1]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            TotalBytes = 1,
        }, cts.Token));
    }

    private static async Task<System.Text.Json.JsonElement> ReadJsonAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var json = await request.Content!.ReadAsStringAsync(cancellationToken);
        return System.Text.Json.JsonDocument.Parse(json).RootElement;
    }

    private static async Task<Dictionary<string, byte[]>> ReadMultipartPartsAsync(HttpContent content, CancellationToken cancellationToken)
    {
        var multipart = Assert.IsType<MultipartFormDataContent>(content);
        var parts = new Dictionary<string, byte[]>();
        foreach (var part in multipart)
        {
            var name = part.Headers.ContentDisposition!.Name!.Trim('"');
            parts[name] = await part.ReadAsByteArrayAsync(cancellationToken);
        }

        return parts;
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));

    /// <summary>Forward-only stream over a fixed buffer - <c>CanSeek</c> is <see langword="false"/>,
    /// same as e.g. a network response stream, to exercise MEDIA-03's required-TotalBytes rule.</summary>
    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override void Flush() => _inner.Flush();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

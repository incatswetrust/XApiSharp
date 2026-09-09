using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Media;
using Xunit.Abstractions;

namespace XApiSharp.SoakTests;

/// <summary>
/// Spec section 19.5: the media half of the required local soak check - a large transfer with
/// fixed buffers, peak memory recorded, live objects (not just RSS) checked after completion. This
/// is the real-load counterpart to <c>MEDIA-01</c>'s design claim ("don't read the whole file into
/// memory") - checked under an actual multi-hundred-megabyte transfer, not just by code inspection.
/// </summary>
public class MediaUploadSoakTests(ITestOutputHelper output)
{
    private const long TotalBytes = 200L * 1024 * 1024; // 200 MiB
    private const int ChunkSizeBytes = 2 * 1024 * 1024; // 2 MiB

    [SoakFact]
    public async Task Large_upload_uses_memory_bounded_by_chunk_size_not_total_transfer_size()
    {
        var appendedSegments = 0;
        long appendedBytesTotal = 0;
        var peakManagedBytes = ForceCollectAndGetTotalMemory();
        using var process = Process.GetCurrentProcess();
        var peakWorkingSetBytes = GetWorkingSetBytes(process);

        var handler = new SoakHttpMessageHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/2/media/upload/initialize")
            {
                return JsonResponse("""{"data":{"id":"soak-1","media_key":"3_soak-1"}}""");
            }

            if (path == "/2/media/upload/soak-1/append")
            {
                // Discard the segment's bytes immediately after counting them - this harness must
                // not itself become the memory hog the test is trying to rule out in the SDK. Only
                // the "media" part counts toward the transfer total - "segment_index" is a small
                // string field, not payload.
                var multipart = (MultipartFormDataContent)request.Content!;
                foreach (var part in multipart)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    var bytes = await part.ReadAsByteArrayAsync(ct);
                    if (name == "media")
                    {
                        appendedBytesTotal += bytes.Length;
                    }
                }

                appendedSegments++;

                // Forced collection when sampling: GC.GetTotalMemory(false) counts allocated-but-
                // not-yet-collected garbage too, which overstates "peak" in a tight loop that
                // outruns the collector - forcing a collection first measures what's actually
                // still live, the number spec 19.5's "peak memory" claim is really about.
                var managedNow = GC.GetTotalMemory(true);
                if (managedNow > peakManagedBytes)
                {
                    peakManagedBytes = managedNow;
                }

                var workingSetNow = GetWorkingSetBytes(process);
                if (workingSetNow > peakWorkingSetBytes)
                {
                    peakWorkingSetBytes = workingSetNow;
                }

                return JsonResponse("""{"data":{}}""");
            }

            if (path == "/2/media/upload/soak-1/finalize")
            {
                return JsonResponse("""{"data":{"id":"soak-1","media_key":"3_soak-1"}}""");
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {path}");
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("token"));

        using var source = new SyntheticByteStream(TotalBytes);
        var progressReports = 0;

        var sw = Stopwatch.StartNew();
        var info = await client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = source,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            TotalBytes = TotalBytes,
            ChunkSizeBytes = ChunkSizeBytes,
            Progress = new Progress<long>(_ => progressReports++),
        });
        sw.Stop();

        Assert.Equal("soak-1", info.Id);
        Assert.Equal(TotalBytes, appendedBytesTotal);
        Assert.Equal((int)((TotalBytes + ChunkSizeBytes - 1) / ChunkSizeBytes), appendedSegments);
        Assert.True(progressReports > 0);

        var afterManagedBytes = ForceCollectAndGetTotalMemory();

        output.WriteLine($"[soak/media] totalBytes={TotalBytes:N0} chunkSizeBytes={ChunkSizeBytes:N0} segments={appendedSegments} duration={sw.Elapsed}");
        output.WriteLine($"[soak/media] managed bytes: peak={peakManagedBytes:N0} after={afterManagedBytes:N0}");
        output.WriteLine($"[soak/media] working set: peak={peakWorkingSetBytes:N0} bytes");

        // MEDIA-01 under real load: peak managed memory should stay a small, bounded multiple of
        // one chunk, nowhere near the 200 MiB transferred - if the whole file were buffered in
        // memory at once, peakManagedBytes would be within a small factor of TotalBytes instead.
        Assert.True(
            peakManagedBytes < ChunkSizeBytes * 10L,
            $"Peak managed memory ({peakManagedBytes:N0} bytes) is more than 10x one chunk ({ChunkSizeBytes:N0} bytes) - looks like the whole transfer was buffered.");
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static long ForceCollectAndGetTotalMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return GC.GetTotalMemory(true);
    }

    private static long GetWorkingSetBytes(Process process)
    {
        process.Refresh();
        return process.WorkingSet64;
    }

    /// <summary>Produces <c>totalBytes</c> of deterministic pseudo-random content directly into
    /// the caller's buffer, never materializing the whole payload anywhere.</summary>
    private sealed class SyntheticByteStream(long totalBytes) : Stream
    {
        private long _produced;
        private byte _counter;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => totalBytes;

        public override long Position
        {
            get => _produced;
            set => throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var remaining = totalBytes - _produced;
            if (remaining <= 0)
            {
                return ValueTask.FromResult(0);
            }

            var toWrite = (int)Math.Min(buffer.Length, remaining);
            var span = buffer.Span[..toWrite];
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = _counter++;
            }

            _produced += toWrite;
            return ValueTask.FromResult(toWrite);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

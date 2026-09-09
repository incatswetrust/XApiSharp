using System.Diagnostics;
using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Streaming;
using Xunit.Abstractions;

namespace XApiSharp.SoakTests;

/// <summary>
/// Spec section 19.5: "perform a local long-running check of a stream and a large media transfer
/// with fixed buffers. Record parameters, peak memory, and the absence of accumulated active
/// connections after stopping. Don't conclude no leak from one RSS figure alone - check live
/// objects/resources after completion." This is the stream half; <see cref="MediaUploadSoakTests"/>
/// is the media half.
/// </summary>
public class StreamingSoakTests(ITestOutputHelper output)
{
    private const int EventCount = 500_000;

    [SoakFact]
    public async Task Long_running_stream_uses_bounded_memory_and_leaves_no_live_connection_after_stop()
    {
        var source = new SyntheticNdjsonStream();
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(source) };
        var connectCount = 0;
        var handler = new SoakHttpMessageHandler((_, _) =>
        {
            connectCount++;
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("token"));

        using var process = Process.GetCurrentProcess();
        var baselineManagedBytes = ForceCollectAndGetTotalMemory();
        var peakManagedBytes = baselineManagedBytes;
        var peakWorkingSetBytes = GetWorkingSetBytes(process);

        var sw = Stopwatch.StartNew();
        long eventsSeen = 0;
        // The source never signals EOF on its own (a real X stream doesn't either) - this loop,
        // not the source, decides when the run ends, matching STREAM-12 (stopping enumeration is
        // a caller decision) instead of relying on the transport closing.
        await foreach (var evt in client.Streaming.StreamPostsSampleAsync(
            new PostSampleStreamRequest(),
            new XStreamOptions<StreamPostResponse> { MaxMessageSizeBytes = 4096 }))
        {
            eventsSeen++;
            _ = evt.Data!.Id;

            if (eventsSeen % 10_000 == 0)
            {
                // Forced collection when sampling (same reasoning as the media soak test): an
                // unforced reading in a tight loop counts allocated-but-not-yet-collected garbage
                // as if it were live, overstating "peak" for memory that was never actually
                // retained.
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
            }

            if (eventsSeen >= EventCount)
            {
                break;
            }
        }

        sw.Stop();

        Assert.Equal(EventCount, eventsSeen);
        Assert.Equal(1, connectCount); // one connection for the whole run, no silent reconnect loop

        // STREAM-12: enumeration ending must close the connection - not "eventually", by the time
        // MoveNextAsync's final call returns. This is the live-object check spec 19.5 asks for
        // (not RSS alone): the SDK's own reference to the connection/stream is gone by now,
        // verified directly rather than inferred from a memory reading - a WeakReference-based
        // check here would only prove whether *this test's own* handler/response closures still
        // reference the stream, which they deliberately do until the method returns, not whether
        // the SDK released its side.
        Assert.True(source.Disposed, "The response's underlying stream was not disposed when enumeration ended.");
        response.Dispose();
        var afterManagedBytes = ForceCollectAndGetTotalMemory();

        output.WriteLine($"[soak/stream] events={EventCount} maxMessageSizeBytes=4096 duration={sw.Elapsed}");
        output.WriteLine($"[soak/stream] managed bytes: baseline={baselineManagedBytes:N0} peak={peakManagedBytes:N0} after={afterManagedBytes:N0} delta(peak-baseline)={peakManagedBytes - baselineManagedBytes:N0}");
        output.WriteLine($"[soak/stream] working set: peak={peakWorkingSetBytes:N0} bytes");
        output.WriteLine($"[soak/stream] connections opened={connectCount}");

        // Peak managed growth should be a small, bounded multiple of one message's size, not
        // proportional to the 500,000 events processed - this is the actual STREAM-04
        // (backpressure/no unbounded accumulation) claim, checked under real load instead of only
        // by code inspection.
        var perEventOverheadIfUnbounded = (peakManagedBytes - baselineManagedBytes) / (double)EventCount;
        Assert.True(perEventOverheadIfUnbounded < 200, $"Peak managed memory grew ~{perEventOverheadIfUnbounded:F1} bytes/event - looks proportional to event count, not bounded.");
    }

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

    /// <summary>Generates an unbounded NDJSON stream directly into the caller's read buffer -
    /// never materializes more than one line anywhere, and never signals EOF on its own (a real X
    /// stream doesn't either) - the consumer, not this source, decides when the run ends.</summary>
    private sealed class SyntheticNdjsonStream : Stream
    {
        private int _emitted;
        private byte[] _pending = [];
        private int _pendingOffset;

        public bool Disposed { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var written = 0;
            while (written < buffer.Length)
            {
                if (_pendingOffset >= _pending.Length)
                {
                    _emitted++;
                    // Every 50th line is a blank heartbeat (STREAM-01), matching real traffic
                    // shape rather than one uniform line over and over.
                    _pending = _emitted % 50 == 0
                        ? "\n"u8.ToArray()
                        : Encoding.UTF8.GetBytes($"{{\"data\":{{\"id\":\"{_emitted}\"}}}}\n");
                    _pendingOffset = 0;
                }

                var toCopy = Math.Min(buffer.Length - written, _pending.Length - _pendingOffset);
                _pending.AsSpan(_pendingOffset, toCopy).CopyTo(buffer.Span[written..]);
                _pendingOffset += toCopy;
                written += toCopy;

                await Task.Yield();
            }

            return written;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}

using System.Text;
using XApiSharp.Errors;
using XApiSharp.Streaming;

namespace XApiSharp.UnitTests;

/// <summary>Unit coverage for the NDJSON line splitter (STREAM-01/02/03). Feeds bytes through a
/// <see cref="ChunkedStream"/> that returns exactly the chunks the test specifies per
/// <c>ReadAsync</c> call, so a line/UTF-8 split across two network reads is reproduced
/// deterministically rather than relying on however a real socket happens to buffer.</summary>
public class XStreamLineReaderTests
{
    [Fact]
    public async Task Reads_a_single_complete_line()
    {
        await using var reader = CreateReader(["hello\n"]);

        Assert.Equal("hello", await reader.ReadLineAsync(default));
        Assert.Null(await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Splits_multiple_lines_delivered_in_one_chunk()
    {
        await using var reader = CreateReader(["a\nb\nc\n"]);

        Assert.Equal("a", await reader.ReadLineAsync(default));
        Assert.Equal("b", await reader.ReadLineAsync(default));
        Assert.Equal("c", await reader.ReadLineAsync(default));
        Assert.Null(await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Strips_a_trailing_carriage_return()
    {
        await using var reader = CreateReader(["hello\r\n"]);

        Assert.Equal("hello", await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Reassembles_a_line_split_across_two_network_reads()
    {
        // STREAM-02: "hel" arrives in one read, "lo\n" in the next.
        await using var reader = CreateReader(["hel", "lo\n"]);

        Assert.Equal("hello", await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Reassembles_a_multi_byte_utf8_character_split_across_two_reads()
    {
        // "café" - the 'é' is 2 UTF-8 bytes (0xC3 0xA9); split the read exactly between them.
        var bytes = Encoding.UTF8.GetBytes("café\n");
        Assert.Equal(0xC3, bytes[3]);
        Assert.Equal(0xA9, bytes[4]);

        var firstChunk = bytes[..4];
        var secondChunk = bytes[4..];
        await using var reader = CreateReaderFromBytes([firstChunk, secondChunk]);

        Assert.Equal("café", await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Returns_a_blank_line_for_a_heartbeat()
    {
        await using var reader = CreateReader(["\r\n", "event\n"]);

        Assert.Equal(string.Empty, await reader.ReadLineAsync(default));
        Assert.Equal("event", await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Returns_the_final_unterminated_line_at_a_clean_end_of_stream()
    {
        await using var reader = CreateReader(["no-trailing-newline"]);

        Assert.Equal("no-trailing-newline", await reader.ReadLineAsync(default));
        Assert.Null(await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Returns_null_immediately_at_a_clean_end_of_stream_with_no_pending_data()
    {
        await using var reader = CreateReader([]);

        Assert.Null(await reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Throws_when_a_line_exceeds_the_configured_maximum_before_a_newline()
    {
        var inner = new ChunkedStream([Encoding.UTF8.GetBytes(new string('x', 20)), Encoding.UTF8.GetBytes("\n")]);
        await using var reader = new XStreamLineReader(inner, maxMessageSizeBytes: 10);

        await Assert.ThrowsAsync<XStreamMessageTooLargeException>(() => reader.ReadLineAsync(default));
    }

    [Fact]
    public async Task Allows_a_line_exactly_at_the_configured_maximum()
    {
        var line = new string('x', 10);
        var inner = new ChunkedStream([Encoding.UTF8.GetBytes(line + "\n")]);
        await using var reader = new XStreamLineReader(inner, maxMessageSizeBytes: 10);

        Assert.Equal(line, await reader.ReadLineAsync(default));
    }

    private static XStreamLineReader CreateReader(IReadOnlyList<string> chunks) =>
        new(new ChunkedStream(chunks.Select(Encoding.UTF8.GetBytes).ToList()), maxMessageSizeBytes: 1024 * 1024);

    private static XStreamLineReader CreateReaderFromBytes(IReadOnlyList<byte[]> chunks) =>
        new(new ChunkedStream(chunks), maxMessageSizeBytes: 1024 * 1024);

    /// <summary>A <see cref="Stream"/> that hands back exactly one caller-specified chunk per
    /// <c>ReadAsync</c> call (never coalescing or further splitting it), so tests can pin down
    /// precisely where a network read boundary falls.</summary>
    private sealed class ChunkedStream(IReadOnlyList<byte[]> chunks) : Stream
    {
        private int _index;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (_index >= chunks.Count)
            {
                return 0;
            }

            var chunk = chunks[_index++];
            chunk.CopyTo(buffer);
            return chunk.Length;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

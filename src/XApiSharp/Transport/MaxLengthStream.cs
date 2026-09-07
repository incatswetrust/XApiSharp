using XApiSharp.Errors;

namespace XApiSharp.Transport;

/// <summary>
/// SER-11: guards non-streaming JSON deserialization against an unbounded response body -
/// without this, a response with no (or a lying) Content-Length header could exhaust memory
/// before <see cref="System.Text.Json.JsonSerializer"/> ever gets to reject it. Streaming
/// endpoints (media download, filtered stream) get their own explicit buffering story in E5;
/// this applies to the ordinary buffered-body path every typed method goes through today.
/// </summary>
internal sealed class MaxLengthStream(Stream inner, long maxLength) : Stream
{
    private long _totalRead;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        _totalRead += read;
        if (_totalRead > maxLength)
        {
            throw new XProtocolException(
                $"Response body exceeded the configured maximum of {maxLength} bytes (XClientOptions.MaxResponseBufferSize).");
        }

        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Flush() => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}

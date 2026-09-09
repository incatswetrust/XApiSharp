namespace XApiSharp.Media;

/// <summary>
/// Read-only decorator that reports cumulative bytes read via <see cref="IProgress{T}"/> as the
/// wrapped <see cref="Stream"/> is consumed (MEDIA-06), without taking ownership of it (MEDIA-02:
/// <see cref="Dispose(bool)"/> never disposes <see cref="_inner"/> - the caller's stream stays
/// open and at the caller's disposal). Works for both seekable and non-seekable inner streams
/// (MEDIA-03) since it only ever reads forward.
/// </summary>
internal sealed class ProgressReportingStream : Stream
{
    private readonly Stream _inner;
    private readonly IProgress<long>? _progress;
    private long _bytesRead;

    public ProgressReportingStream(Stream inner, IProgress<long>? progress)
    {
        _inner = inner;
        _progress = progress;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _bytesRead;
        set => throw new NotSupportedException("ProgressReportingStream is forward-read-only.");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        Report(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        Report(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Report(read);
        return read;
    }

    private void Report(int bytesJustRead)
    {
        if (bytesJustRead <= 0)
        {
            return;
        }

        _bytesRead += bytesJustRead;

        // MEDIA-06: a throwing callback must not be silently swallowed - let it propagate out of
        // the read that triggered it.
        _progress?.Report(_bytesRead);
    }

    public override void Flush() => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        // MEDIA-02: the wrapped stream is caller-owned - never dispose it here, only the base
        // Stream's own state.
        base.Dispose(disposing);
    }
}

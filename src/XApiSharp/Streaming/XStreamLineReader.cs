using System.Text;

namespace XApiSharp.Streaming;

/// <summary>
/// Splits a raw response <see cref="Stream"/> into NDJSON lines (STREAM-01), correctly
/// reassembling a line/UTF-8 character split across two network reads (STREAM-02) by never
/// decoding a byte as text until the complete line (up to and including the terminating
/// <c>\n</c>) has been buffered - so a multi-byte UTF-8 sequence split mid-character across reads
/// is always decoded whole, never half-decoded. Caps the buffered-but-unterminated byte count at
/// <see cref="_maxMessageSizeBytes"/> (STREAM-03) rather than growing it without bound.
/// </summary>
internal sealed class XStreamLineReader : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly int _maxMessageSizeBytes;
    private readonly byte[] _readBuffer;
    private int _readBufferLength;
    private int _readBufferOffset;
    private readonly MemoryStream _pendingLine = new();

    public XStreamLineReader(Stream stream, int maxMessageSizeBytes, int readChunkSizeBytes = 8192)
    {
        _stream = stream;
        _maxMessageSizeBytes = maxMessageSizeBytes;
        _readBuffer = new byte[readChunkSizeBytes];
    }

    /// <summary>The next complete line with any trailing <c>\r\n</c>/<c>\n</c> stripped
    /// (a blank result is a heartbeat/keep-alive line, not absence of data), or
    /// <see langword="null"/> at a clean end-of-stream.</summary>
    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (_readBufferOffset < _readBufferLength)
            {
                var newlineIndex = Array.IndexOf(_readBuffer, (byte)'\n', _readBufferOffset, _readBufferLength - _readBufferOffset);
                if (newlineIndex >= 0)
                {
                    AppendPending(_readBufferOffset, newlineIndex - _readBufferOffset);
                    _readBufferOffset = newlineIndex + 1;
                    return ExtractPendingLine();
                }

                AppendPending(_readBufferOffset, _readBufferLength - _readBufferOffset);
                _readBufferOffset = _readBufferLength;
            }

            _readBufferLength = await _stream.ReadAsync(_readBuffer, cancellationToken).ConfigureAwait(false);
            _readBufferOffset = 0;

            if (_readBufferLength == 0)
            {
                return _pendingLine.Length > 0 ? ExtractPendingLine() : null;
            }
        }
    }

    private void AppendPending(int offset, int count)
    {
        if (count == 0)
        {
            return;
        }

        if (_pendingLine.Length + count > _maxMessageSizeBytes)
        {
            throw new Errors.XStreamMessageTooLargeException(
                $"A single stream message exceeded the configured maximum of {_maxMessageSizeBytes} bytes (XStreamOptions.MaxMessageSizeBytes).");
        }

        _pendingLine.Write(_readBuffer, offset, count);
    }

    private string ExtractPendingLine()
    {
        var buffer = _pendingLine.GetBuffer();
        var length = (int)_pendingLine.Length;
        if (length > 0 && buffer[length - 1] == (byte)'\r')
        {
            length--;
        }

        var text = Encoding.UTF8.GetString(buffer, 0, length);
        _pendingLine.SetLength(0);
        return text;
    }

    public async ValueTask DisposeAsync()
    {
        await _pendingLine.DisposeAsync().ConfigureAwait(false);
    }
}

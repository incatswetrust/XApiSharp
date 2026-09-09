namespace XApiSharp.Errors;

/// <summary>A single stream line (message) exceeded
/// <see cref="XApiSharp.Streaming.XStreamOptions{TBody}.MaxMessageSizeBytes"/> before a newline
/// was found (STREAM-03) - the connection is not further read past this point; never retried by
/// reconnect (STREAM-11: not a transient network condition).</summary>
public sealed class XStreamMessageTooLargeException : XApiException
{
    public XStreamMessageTooLargeException(string message)
        : base(message, statusCode: null, problem: null, requestId: null, innerException: null)
    {
    }
}

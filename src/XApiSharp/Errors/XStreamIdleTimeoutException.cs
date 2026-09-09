namespace XApiSharp.Errors;

/// <summary>No data (including heartbeats) arrived within the configured
/// <see cref="XApiSharp.Streaming.XStreamOptions{TBody}.IdleTimeout"/> (STREAM-08). Not thrown
/// unless the caller opts in - the SDK doesn't invent a default heartbeat interval per stream.</summary>
public sealed class XStreamIdleTimeoutException : XApiException
{
    public XStreamIdleTimeoutException(string message)
        : base(message, statusCode: null, problem: null, requestId: null, innerException: null)
    {
    }
}

namespace XApiSharp.Errors;

/// <summary>
/// The stream connection ended unexpectedly mid-enumeration (STREAM-06) - either a transport-level
/// read failure, or the server closing the connection cleanly while the client was still
/// iterating. The registry doesn't declare an explicit "server disconnect command" message shape
/// for any in-scope streaming operation (API-10: not invented here), so both causes surface as
/// this one type rather than a false discrimination between them; <see cref="Exception.InnerException"/>
/// carries the underlying transport error when there was one. Eligible for reconnect under an
/// opt-in <see cref="XApiSharp.Streaming.XStreamReconnectOptions"/> (STREAM-07); never thrown for
/// caller-driven cancellation, which surfaces as <see cref="OperationCanceledException"/> instead.
/// </summary>
public sealed class XStreamConnectionLostException : XApiException
{
    public XStreamConnectionLostException(string message, Exception? innerException = null)
        : base(message, statusCode: null, problem: null, requestId: null, innerException)
    {
    }
}

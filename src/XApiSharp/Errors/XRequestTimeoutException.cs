namespace XApiSharp.Errors;

/// <summary>The SDK's own internal operation deadline was exceeded (spec section 12.2) - not a
/// cancellation requested by the caller (that surfaces as <see cref="OperationCanceledException"/>).</summary>
public sealed class XRequestTimeoutException : XApiException
{
    public XRequestTimeoutException(string message, Exception? innerException = null)
        : base(message, statusCode: null, problem: null, requestId: null, innerException)
    {
    }
}

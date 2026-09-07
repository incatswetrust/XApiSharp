namespace XApiSharp.Errors;

/// <summary>Transport-level failure with no HTTP response at all (spec section 12.2) - DNS,
/// connection refused, TLS failure, etc. The original cause is preserved as InnerException.</summary>
public sealed class XTransportException : XApiException
{
    public XTransportException(string message, Exception innerException)
        : base(message, statusCode: null, problem: null, requestId: null, innerException)
    {
    }
}

namespace XApiSharp.Errors;

/// <summary>A stream line wasn't valid JSON for the expected contract (STREAM-11) - propagated
/// out of the enumeration rather than silently skipped or masked by reconnect; a malformed
/// message is a contract violation, not a transient network condition.</summary>
public sealed class XStreamMalformedMessageException : XApiException
{
    public XStreamMalformedMessageException(string message, Exception innerException)
        : base(message, statusCode: null, problem: null, requestId: null, innerException)
    {
    }
}

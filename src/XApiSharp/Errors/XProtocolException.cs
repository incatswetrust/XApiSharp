using System.Net;

namespace XApiSharp.Errors;

/// <summary>The response didn't match the expected contract for the operation - e.g. HTML,
/// empty body where a body was required, or malformed JSON (spec section 12.2).</summary>
public sealed class XProtocolException : XApiException
{
    /// <summary>A bounded diagnostic excerpt of the unexpected body. Not the full raw body by
    /// default - see spec section 12.2 on gated raw-body diagnostics.</summary>
    public string? DiagnosticExcerpt { get; init; }

    public XProtocolException(string message, HttpStatusCode? statusCode = null, string? requestId = null, Exception? innerException = null)
        : base(message, statusCode, problem: null, requestId, innerException)
    {
    }
}

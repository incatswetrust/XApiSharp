using System.Net;

namespace XApiSharp.Errors;

/// <summary>Insufficient scopes or access for the requested operation (spec section 12.2).</summary>
public sealed class XAccessDeniedException : XApiException
{
    public XAccessDeniedException(string message, HttpStatusCode? statusCode = null, XProblem? problem = null, string? requestId = null, Exception? innerException = null)
        : base(message, statusCode, problem, requestId, innerException)
    {
    }
}

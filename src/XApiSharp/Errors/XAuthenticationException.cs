using System.Net;

namespace XApiSharp.Errors;

/// <summary>Auth was invalid or expired (spec section 12.2).</summary>
public sealed class XAuthenticationException : XApiException
{
    public XAuthenticationException(string message, HttpStatusCode? statusCode = null, XProblem? problem = null, string? requestId = null, Exception? innerException = null)
        : base(message, statusCode, problem, requestId, innerException)
    {
    }
}

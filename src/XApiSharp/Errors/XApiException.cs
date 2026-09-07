using System.Net;

namespace XApiSharp.Errors;

/// <summary>
/// Root of the SDK's exception hierarchy (spec section 12.2). Thrown for a non-success HTTP
/// status; derived types narrow the situation further. Carries the original problem structure
/// where the server provided one - never leaks raw response bodies by default.
/// </summary>
public class XApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public XProblem? Problem { get; }

    public string? RequestId { get; }

    public XApiException(string message, HttpStatusCode? statusCode = null, XProblem? problem = null, string? requestId = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Problem = problem;
        RequestId = requestId;
    }
}

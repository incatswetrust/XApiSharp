using System.Net;

namespace XApiSharp.Errors;

/// <summary>Temporary rate limit (spec section 12.2). <see cref="RetryAfter"/> is populated
/// when the server provided a usable hint (RATE-01); never guessed when absent (RATE-02).</summary>
public sealed class XRateLimitException : XApiException
{
    public TimeSpan? RetryAfter { get; init; }

    public XRateLimitException(string message, HttpStatusCode? statusCode = null, XProblem? problem = null, string? requestId = null, Exception? innerException = null)
        : base(message, statusCode, problem, requestId, innerException)
    {
    }
}

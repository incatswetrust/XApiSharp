namespace XApiSharp.Errors;

/// <summary>PAGE-06: a paginated enumeration saw the same continuation token twice, meaning the
/// endpoint is cycling instead of terminating. Thrown from within the <c>await foreach</c> rather
/// than looping forever. The token is carried opaquely (PAGE-10) - never parsed or decoded.</summary>
public sealed class XTokenCycleException : XApiException
{
    /// <summary>The repeated pagination token, exactly as received from the server.</summary>
    public string Token { get; }

    public XTokenCycleException(string message, string token)
        : base(message)
    {
        Token = token;
    }
}

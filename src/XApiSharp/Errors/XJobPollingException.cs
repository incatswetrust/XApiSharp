namespace XApiSharp.Errors;

/// <summary>
/// Thrown by <c>ComplianceClient.WaitForCompletionAsync</c> (spec section 17.2) when polling ends
/// without the job reaching <c>complete</c> - either the job itself reported <c>failed</c>, or the
/// wait deadline was reached first. <see cref="LastKnownStatus"/> preserves whatever status was
/// last observed so the caller isn't left guessing; the SDK never resubmits the original job on
/// your behalf (spec 17.2: "don't resubmit a job when the outcome of the first request is unknown") -
/// deciding whether creating a new one is safe is a caller decision, not something this exception
/// or the polling loop do automatically.
/// </summary>
public sealed class XJobPollingException : XApiException
{
    public string JobId { get; }

    /// <summary>The job's <c>status</c> value as last observed, or <see langword="null"/> if a
    /// status read never returned any data at all.</summary>
    public string? LastKnownStatus { get; }

    public XJobPollingException(string message, string jobId, string? lastKnownStatus)
        : base(message)
    {
        JobId = jobId;
        LastKnownStatus = lastKnownStatus;
    }
}

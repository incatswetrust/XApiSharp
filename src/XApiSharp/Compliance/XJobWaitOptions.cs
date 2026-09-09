using XApiSharp.Common;

namespace XApiSharp.Compliance;

/// <summary>
/// Tuning for <c>ComplianceClient.WaitForCompletionAsync</c> (spec section 17.2). The registry's
/// job-status schema has no server-provided poll-interval hint (unlike Media's
/// <c>check_after_secs</c>) - <see cref="PollInterval"/> is a documented, overridable fixed
/// default rather than something invented to look server-driven.
/// </summary>
public sealed class XJobWaitOptions
{
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Total time to keep polling before giving up. Compliance jobs can legitimately
    /// take a long time on large exports - the 30-minute default is a starting point, not a
    /// registry-declared bound; override it for your own workload.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(30);

    public IReadOnlyCollection<XComplianceJobField>? Fields { get; init; }
}

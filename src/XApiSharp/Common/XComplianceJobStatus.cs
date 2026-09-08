namespace XApiSharp.Common;

/// <summary>The <c>status</c> filter on <c>GET /2/compliance/jobs</c>.</summary>
public enum XComplianceJobStatus
{
    Created,
    InProgress,
    Failed,
    Complete,
}

public static class XComplianceJobStatusExtensions
{
    public static string ToApiValue(this XComplianceJobStatus value) => value switch
    {
        XComplianceJobStatus.Created => "created",
        XComplianceJobStatus.InProgress => "in_progress",
        XComplianceJobStatus.Failed => "failed",
        XComplianceJobStatus.Complete => "complete",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

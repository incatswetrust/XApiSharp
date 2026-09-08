namespace XApiSharp.Common;

/// <summary>The <c>usage.fields</c> query parameter (SER-05).</summary>
public enum XUsageField
{
    CapResetDay,
    DailyClientAppUsage,
    DailyProjectUsage,
    ProjectCap,
    ProjectId,
    ProjectUsage,
}

public static class XUsageFieldExtensions
{
    public static string ToApiValue(this XUsageField field) => field switch
    {
        XUsageField.CapResetDay => "cap_reset_day",
        XUsageField.DailyClientAppUsage => "daily_client_app_usage",
        XUsageField.DailyProjectUsage => "daily_project_usage",
        XUsageField.ProjectCap => "project_cap",
        XUsageField.ProjectId => "project_id",
        XUsageField.ProjectUsage => "project_usage",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

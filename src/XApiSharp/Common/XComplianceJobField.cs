namespace XApiSharp.Common;

/// <summary>The <c>compliance_job.fields</c> query parameter (SER-05).</summary>
public enum XComplianceJobField
{
    CreatedAt,
    DownloadExpiresAt,
    DownloadUrl,
    Id,
    Name,
    Resumable,
    Status,
    Type,
    UploadExpiresAt,
    UploadUrl,
}

public static class XComplianceJobFieldExtensions
{
    public static string ToApiValue(this XComplianceJobField field) => field switch
    {
        XComplianceJobField.CreatedAt => "created_at",
        XComplianceJobField.DownloadExpiresAt => "download_expires_at",
        XComplianceJobField.DownloadUrl => "download_url",
        XComplianceJobField.Id => "id",
        XComplianceJobField.Name => "name",
        XComplianceJobField.Resumable => "resumable",
        XComplianceJobField.Status => "status",
        XComplianceJobField.Type => "type",
        XComplianceJobField.UploadExpiresAt => "upload_expires_at",
        XComplianceJobField.UploadUrl => "upload_url",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

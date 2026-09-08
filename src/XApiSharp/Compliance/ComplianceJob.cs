using System.Text.Json.Serialization;

namespace XApiSharp.Compliance;

/// <summary>
/// Modeled from the "ComplianceJob" schema. Job-status polling only, per the E4 scope - the
/// long-running "wait for completion with cancellation/deadline" convenience helper (spec section
/// 17.2) lands with the rest of the jobs/long-operations work in E5, alongside Media/Streaming/
/// Webhooks. <c>status</c>/<c>type</c> stay plain strings (SER-06) rather than the closed
/// <see cref="Common.XComplianceJobStatus"/>/<see cref="Common.XComplianceJobType"/> enums used on
/// the *request* side - a response value should tolerate the server adding a new one without
/// breaking deserialization.
/// </summary>
public sealed class ComplianceJob
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("resumable")]
    public bool? Resumable { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("upload_url")]
    public string? UploadUrl { get; init; }

    [JsonPropertyName("upload_expires_at")]
    public DateTimeOffset? UploadExpiresAt { get; init; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; init; }

    [JsonPropertyName("download_expires_at")]
    public DateTimeOffset? DownloadExpiresAt { get; init; }
}

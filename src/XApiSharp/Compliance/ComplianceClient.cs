using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Compliance;

/// <summary>
/// Typed methods for the Compliance family (6 operations per the registry) - job-status polling
/// only. The long-running "wait for completion" convenience helper (spec section 17.2) lands with
/// the rest of the jobs/long-operations work in E5.
/// </summary>
public sealed class ComplianceClient
{
    private readonly RequestExecutor _executor;

    internal ComplianceClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/compliance/jobs</c> - Get Compliance Jobs. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<GetJobsResponse>> GetJobsAsync(GetJobsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("type", request.Type.ToApiValue()),
            ("status", request.Status?.ToApiValue()),
            ("compliance_job.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetJobsResponse>(HttpMethod.Get, "2/compliance/jobs", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/compliance/jobs</c> - Create Compliance Job. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<CreateJobResponse>> CreateJobAsync(CreateJobRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("compliance_job.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };
        var body = new CreateJobBody { Type = request.Type.ToApiValue(), Name = request.Name, Resumable = request.Resumable };

        return _executor.SendAsync<CreateJobResponse>(HttpMethod.Post, "2/compliance/jobs", body, query, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/compliance/jobs/{id}</c> - Get Compliance Job by ID. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<GetJobByIdResponse>> GetJobByIdAsync(GetJobByIdRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("compliance_job.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetJobByIdResponse>(
            HttpMethod.Get,
            $"2/compliance/jobs/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/compliance/jobs/{id}</c> - Cancel Compliance Job. Requires app-only bearer.
    /// </summary>
    public Task<XResponse<CancelJobResponse>> CancelJobAsync(CancelJobRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("compliance_job.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<CancelJobResponse>(
            HttpMethod.Delete,
            $"2/compliance/jobs/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/compliance/jobs/{id}/download</c> - Download Compliance Job Results. Requires
    /// app-only bearer. Not JSON (spec section 9) - returns raw bytes.
    /// </summary>
    public Task<XResponse<byte[]>> DownloadJobResultsAsync(DownloadJobResultsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Token);

        var query = new List<(string Name, string? Value)>
        {
            ("token", request.Token),
        };

        return _executor.SendForBytesAsync(
            HttpMethod.Get,
            $"2/compliance/jobs/{Uri.EscapeDataString(request.Id)}/download",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>PUT /2/compliance/jobs/{id}/upload</c> - Upload Compliance Job Submission. Requires
    /// app-only bearer. Not JSON (spec section 9) - sends raw bytes.
    /// </summary>
    public Task<XResponse<UploadJobSubmissionResponse>> UploadJobSubmissionAsync(UploadJobSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Token);
        ArgumentNullException.ThrowIfNull(request.Content);

        var query = new List<(string Name, string? Value)>
        {
            ("token", request.Token),
        };

        return _executor.SendBytesAsync<UploadJobSubmissionResponse>(
            HttpMethod.Put,
            $"2/compliance/jobs/{Uri.EscapeDataString(request.Id)}/upload",
            request.Content,
            query,
            cancellationToken);
    }
}

using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Transport;

namespace XApiSharp.Compliance;

/// <summary>
/// Typed methods for the Compliance family (6 operations per the registry), plus the "wait for
/// completion" polling convenience over them (spec section 17.2).
/// </summary>
public sealed class ComplianceClient
{
    private readonly RequestExecutor _executor;
    private readonly TimeProvider _timeProvider;

    internal ComplianceClient(RequestExecutor executor, TimeProvider timeProvider)
    {
        _executor = executor;
        _timeProvider = timeProvider;
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

    /// <summary>
    /// Polls <c>GET /2/compliance/jobs/{id}</c> until the job reaches <c>complete</c> or the
    /// deadline/cancellation fires (spec section 17.2). Throws <see cref="XJobPollingException"/>
    /// if the job reports <c>failed</c> or the deadline is reached first - <see cref="XJobPollingException.LastKnownStatus"/>
    /// preserves whatever status was last observed. Per HTTP-11, the returned job's
    /// <see cref="ComplianceJob.DownloadUrl"/>/<see cref="ComplianceJob.UploadUrl"/> are opaque,
    /// caller-followed links - this method never fetches them, and following one yourself must not
    /// attach the X <c>Authorization</c> header (it may be a signed third-party storage URL, not
    /// an X API host). Never resubmits <see cref="CreateJobAsync"/> on your behalf - deciding
    /// whether creating a new job is safe after an unknown outcome is a caller decision (spec
    /// 17.2: "don't resubmit a job when the outcome of the first request is unknown").
    /// </summary>
    public async Task<ComplianceJob> WaitForCompletionAsync(string jobId, XJobWaitOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        options ??= new XJobWaitOptions();

        var deadline = _timeProvider.GetUtcNow() + options.Timeout;

        while (true)
        {
            var response = await GetJobByIdAsync(new GetJobByIdRequest { Id = jobId, Fields = options.Fields }, cancellationToken).ConfigureAwait(false);
            var job = response.Body?.Data
                ?? throw new XJobPollingException($"Get compliance job by id returned no data for job '{jobId}'.", jobId, lastKnownStatus: null);

            if (job.Status == "complete")
            {
                return job;
            }

            if (job.Status == "failed")
            {
                throw new XJobPollingException($"Compliance job '{jobId}' failed.", jobId, job.Status);
            }

            if (_timeProvider.GetUtcNow() + options.PollInterval > deadline)
            {
                throw new XJobPollingException($"Timed out waiting for compliance job '{jobId}' to complete.", jobId, job.Status);
            }

            await Task.Delay(options.PollInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}

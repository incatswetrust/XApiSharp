namespace XApiSharp.Compliance;

/// <summary>Request for <c>GET /2/compliance/jobs/{id}/download</c>. Not JSON - the registry
/// declares <c>application/octet-stream</c>, so <c>ComplianceClient.DownloadJobResultsAsync</c>
/// returns raw bytes (spec section 9), same treatment as
/// <see cref="DirectMessages.DirectMessagesClient.DownloadMediaAsync"/>.</summary>
public sealed class DownloadJobResultsRequest
{
    public required string Id { get; init; }

    /// <summary>The signed download token from <see cref="ComplianceJob.DownloadUrl"/> - opaque,
    /// server-issued (PAGE-10's "don't parse an opaque token" applies equally here).</summary>
    public required string Token { get; init; }
}

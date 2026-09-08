using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Compliance;

/// <summary>Request for <c>PUT /2/compliance/jobs/{id}/upload</c>. Not JSON - the registry
/// declares an <c>application/octet-stream</c> request body, so <see cref="Content"/> is raw
/// bytes rather than a typed model (spec section 9).</summary>
public sealed class UploadJobSubmissionRequest
{
    public required string Id { get; init; }

    /// <summary>The signed upload token from <see cref="ComplianceJob.UploadUrl"/> - opaque,
    /// server-issued (PAGE-10).</summary>
    public required string Token { get; init; }

    public required byte[] Content { get; init; }
}

/// <summary>Modeled from the "UploadComplianceJobSubmissionResponse" schema.</summary>
public sealed class UploadJobSubmissionResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ComplianceJob? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Compliance;

/// <summary>Request for <c>DELETE /2/compliance/jobs/{id}</c> - cancels the job (the registry
/// calls this "Cancel Compliance Job", not "Delete" - the response still returns the job's own
/// shape, not a boolean deleted flag).</summary>
public sealed class CancelJobRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XComplianceJobField>? Fields { get; init; }
}

/// <summary>Modeled from the "DeleteComplianceJobsByIdResponse" schema.</summary>
public sealed class CancelJobResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ComplianceJob? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

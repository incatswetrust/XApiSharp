using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Compliance;

/// <summary>Request for <c>GET /2/compliance/jobs</c>. Not paginated - the registry declares only
/// a <c>result_count</c> hint, no continuation token.</summary>
public sealed class GetJobsRequest
{
    public required XComplianceJobType Type { get; init; }

    public XComplianceJobStatus? Status { get; init; }

    public IReadOnlyCollection<XComplianceJobField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetComplianceJobsResponse" schema.</summary>
public sealed class GetJobsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ComplianceJob>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public ComplianceJobsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class ComplianceJobsMeta
{
    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

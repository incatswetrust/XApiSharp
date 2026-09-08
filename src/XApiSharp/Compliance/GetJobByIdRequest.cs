using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Compliance;

/// <summary>Request for <c>GET /2/compliance/jobs/{id}</c>.</summary>
public sealed class GetJobByIdRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XComplianceJobField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetComplianceJobsByIdResponse" schema.</summary>
public sealed class GetJobByIdResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ComplianceJob? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

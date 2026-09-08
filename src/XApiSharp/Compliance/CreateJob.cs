using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Compliance;

/// <summary>Request for <c>POST /2/compliance/jobs</c>.</summary>
public sealed class CreateJobRequest
{
    public required XComplianceJobType Type { get; init; }

    /// <summary>Up to 64 characters, per the registry.</summary>
    public string? Name { get; init; }

    public bool? Resumable { get; init; }

    public IReadOnlyCollection<XComplianceJobField>? Fields { get; init; }
}

/// <summary>Modeled from the "CreateComplianceJobsResponse" schema.</summary>
public sealed class CreateJobResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ComplianceJob? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class CreateJobBody
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("resumable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Resumable { get; init; }
}

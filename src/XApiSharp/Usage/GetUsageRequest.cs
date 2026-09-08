using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Usage;

/// <summary>Request for <c>GET /2/usage/tweets</c>.</summary>
public sealed class GetUsageRequest
{
    /// <summary>1-90, per the registry (default 7).</summary>
    public int? Days { get; init; }

    public IReadOnlyCollection<XUsageField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetUsageResponse" schema.</summary>
public sealed class GetUsageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public Usage? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

/// <summary>Modeled from the "Usage" schema. <c>daily_client_app_usage</c>/
/// <c>daily_project_usage</c> stay <see cref="JsonElement"/> (SER-09) - nested per-day
/// breakdowns.</summary>
public sealed class Usage
{
    [JsonPropertyName("project_id")]
    public string? ProjectId { get; init; }

    [JsonPropertyName("project_cap")]
    public string? ProjectCap { get; init; }

    [JsonPropertyName("project_usage")]
    public string? ProjectUsage { get; init; }

    [JsonPropertyName("cap_reset_day")]
    public int? CapResetDay { get; init; }

    [JsonPropertyName("daily_client_app_usage")]
    public JsonElement? DailyClientAppUsage { get; init; }

    [JsonPropertyName("daily_project_usage")]
    public JsonElement? DailyProjectUsage { get; init; }
}

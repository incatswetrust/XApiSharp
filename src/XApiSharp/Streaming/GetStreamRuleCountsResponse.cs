using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>Modeled from the "GetRuleCountsResponse" schema.</summary>
public sealed class GetStreamRuleCountsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public StreamRuleCounts? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class StreamRuleCounts
{
    [JsonPropertyName("cap_per_client_app")]
    public string? CapPerClientApp { get; init; }

    [JsonPropertyName("cap_per_project")]
    public string? CapPerProject { get; init; }

    [JsonPropertyName("project_rules_count")]
    public string? ProjectRulesCount { get; init; }

    [JsonPropertyName("client_app_rules_count")]
    public StreamClientAppRuleCount? ClientAppRulesCount { get; init; }

    [JsonPropertyName("all_project_client_apps")]
    public IReadOnlyList<StreamClientAppRuleCount>? AllProjectClientApps { get; init; }
}

public sealed class StreamClientAppRuleCount
{
    [JsonPropertyName("client_app_id")]
    public string? ClientAppId { get; init; }

    [JsonPropertyName("rule_count")]
    public int? RuleCount { get; init; }
}

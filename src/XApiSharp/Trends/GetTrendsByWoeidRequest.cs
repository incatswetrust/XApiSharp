using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Trends;

/// <summary>Request for <c>GET /2/trends/by/woeid/{woeid}</c> - the "Where On Earth" location
/// identifier the trends are scoped to.</summary>
public sealed class GetTrendsByWoeidRequest
{
    public required int Woeid { get; init; }

    /// <summary>1-50, per the registry.</summary>
    public int? MaxTrends { get; init; }

    public IReadOnlyCollection<XTrendField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetTrendsByWoeidResponse" schema.</summary>
public sealed class GetTrendsByWoeidResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Trend>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

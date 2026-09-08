using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Trends;

/// <summary>Request for <c>GET /2/users/personalized_trends</c>. No <c>id</c> path parameter -
/// operates on the authenticated user, like <see cref="XApiSharp.Users.GetMyUserRequest"/>.</summary>
public sealed class GetPersonalizedTrendsRequest
{
    public IReadOnlyCollection<XPersonalizedTrendField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetTrendsPersonalizedTrendsResponse" schema.</summary>
public sealed class GetPersonalizedTrendsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<PersonalizedTrend>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

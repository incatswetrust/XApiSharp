using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Communities;

/// <summary>Request for <c>GET /2/communities/{id}</c>.</summary>
public sealed class GetCommunityRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XCommunityField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetCommunitiesByIdResponse" schema.</summary>
public sealed class GetCommunityResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public Community? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

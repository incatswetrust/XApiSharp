using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.News;

/// <summary>Request for <c>GET /2/news/{id}</c>.</summary>
public sealed class GetNewsRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XNewsField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetNewsResponse" schema.</summary>
public sealed class GetNewsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public News? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

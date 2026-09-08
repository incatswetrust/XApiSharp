using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Spaces;

/// <summary>Modeled from the "GetSpacesByIdResponse" schema.</summary>
public sealed class GetSpaceResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public Space? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

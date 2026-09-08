using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Spaces;

/// <summary>Shared response shape for <c>GET /2/spaces</c>, <c>GET /2/spaces/by/creator_ids</c>,
/// and <c>GET /2/spaces/search</c> - none of these are paginated (no token in the registry, just
/// an optional <see cref="SpacesMeta.ResultCount"/> hint on two of the three; <c>GET /2/spaces</c>
/// declares no <c>meta</c> at all, which simply deserializes to a null <see cref="Meta"/>).</summary>
public sealed class GetSpacesListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Space>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public SpacesMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class SpacesMeta
{
    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

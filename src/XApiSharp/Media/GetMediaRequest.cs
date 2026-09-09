using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>GET /2/media/{media_key}</c>.</summary>
public sealed class GetMediaRequest
{
    public required string MediaKey { get; init; }

    public IReadOnlyCollection<XMediaField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetMediaByMediaKeyResponse" schema.</summary>
public sealed class GetMediaResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public Common.Media? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

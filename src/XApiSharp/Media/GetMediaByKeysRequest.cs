using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>GET /2/media</c> - up to 100 media keys per call, per the registry.</summary>
public sealed class GetMediaByKeysRequest
{
    public required IReadOnlyCollection<string> MediaKeys { get; init; }

    public IReadOnlyCollection<XMediaField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetMediaByMediaKeysResponse" schema.</summary>
public sealed class GetMediaByKeysResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Common.Media>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

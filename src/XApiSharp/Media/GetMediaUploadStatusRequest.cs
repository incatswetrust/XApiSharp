using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>GET /2/media/upload</c>.</summary>
public sealed class GetMediaUploadStatusRequest
{
    public required string MediaId { get; init; }
}

/// <summary>Modeled from the "GetMediaUploadStatusResponse" schema.</summary>
public sealed class GetMediaUploadStatusResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MediaUploadInfo? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

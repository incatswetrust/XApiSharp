using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>POST /2/media/upload/{id}/finalize</c>.</summary>
public sealed class FinalizeMediaUploadRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "FinalizeMediaUploadResponse" schema.</summary>
public sealed class FinalizeMediaUploadResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MediaUploadInfo? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

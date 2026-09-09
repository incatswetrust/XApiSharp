using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Modeled from the "MediaUploadResponse" schema - identical shape to
/// <see cref="GetMediaUploadStatusResponse"/>/<see cref="FinalizeMediaUploadResponse"/>, hence
/// the shared <see cref="MediaUploadInfo"/> data type.</summary>
public sealed class UploadMediaResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MediaUploadInfo? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

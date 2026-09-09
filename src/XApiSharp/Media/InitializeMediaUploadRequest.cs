using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>
/// Request for <c>POST /2/media/upload/initialize</c> - the first step of the chunked upload
/// sequence (spec section 15.1). <see cref="TotalBytes"/> is required by
/// <see cref="MediaClient.InitializeUploadAsync"/> even though the registry marks it optional -
/// MEDIA-03 requires the known-length requirement be explained up front rather than discovered
/// as a late server error; the high-level <c>UploadAsync</c> facade computes it from the source
/// <see cref="System.IO.Stream"/> when possible and fails fast with a clear message when it can't
/// (a non-seekable stream with no caller-supplied length).
/// </summary>
public sealed class InitializeMediaUploadRequest
{
    public required XMediaCategory MediaCategory { get; init; }

    public required XMediaMimeType MediaType { get; init; }

    /// <summary>0-17,179,869,184 bytes (16 GiB), per the registry.</summary>
    public required long TotalBytes { get; init; }

    public IReadOnlyCollection<string>? AdditionalOwners { get; init; }

    public bool? Shared { get; init; }
}

/// <summary>Modeled from the "InitializeMediaUploadResponse" schema.</summary>
public sealed class InitializeMediaUploadResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public MediaUploadInfo? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class InitializeMediaUploadBody
{
    [JsonPropertyName("media_category")]
    public required string MediaCategory { get; init; }

    [JsonPropertyName("media_type")]
    public required string MediaType { get; init; }

    [JsonPropertyName("total_bytes")]
    public required long TotalBytes { get; init; }

    [JsonPropertyName("additional_owners")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? AdditionalOwners { get; init; }

    [JsonPropertyName("shared")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Shared { get; init; }
}

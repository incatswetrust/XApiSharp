using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Modeled from the "AppendMediaUploadResponse" schema.</summary>
public sealed class AppendMediaUploadResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public AppendMediaUploadResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class AppendMediaUploadResponseData
{
    /// <summary>Epoch seconds when the upload session expires, per the registry.</summary>
    [JsonPropertyName("expires_at")]
    public long? ExpiresAt { get; init; }
}

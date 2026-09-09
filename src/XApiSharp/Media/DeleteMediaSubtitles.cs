using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>DELETE /2/media/subtitles</c>.</summary>
public sealed class DeleteMediaSubtitlesRequest
{
    public required string Id { get; init; }

    /// <summary>No documented closed enum in the registry for this operation specifically -
    /// raw passthrough (unlike <see cref="CreateMediaSubtitlesRequest.MediaCategory"/>, which
    /// does declare one).</summary>
    public required string MediaCategory { get; init; }

    /// <summary>BCP47 language code.</summary>
    public required string LanguageCode { get; init; }
}

/// <summary>Modeled from the "DeleteMediaSubtitlesResponse" schema.</summary>
public sealed class DeleteMediaSubtitlesResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteMediaSubtitlesResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteMediaSubtitlesResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

internal sealed class DeleteMediaSubtitlesBody
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("media_category")]
    public required string MediaCategory { get; init; }

    [JsonPropertyName("language_code")]
    public required string LanguageCode { get; init; }
}

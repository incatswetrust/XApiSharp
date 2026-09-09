using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>Request for <c>POST /2/media/subtitles</c>.</summary>
public sealed class CreateMediaSubtitlesRequest
{
    public required string Id { get; init; }

    public XSubtitlesMediaCategory? MediaCategory { get; init; }

    public required IReadOnlyCollection<MediaSubtitle> Subtitles { get; init; }
}

public sealed class MediaSubtitle
{
    public required string Id { get; init; }

    /// <summary>BCP47 language code (two uppercase letters, per the registry's pattern).</summary>
    public required string LanguageCode { get; init; }

    /// <summary>Human-readable language name.</summary>
    public string? DisplayName { get; init; }
}

/// <summary>Modeled from the "CreateMediaSubtitlesResponse" schema.</summary>
public sealed class CreateMediaSubtitlesResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateMediaSubtitlesResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateMediaSubtitlesResponseData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("media_category")]
    public string? MediaCategory { get; init; }

    [JsonPropertyName("associated_subtitles")]
    public JsonElement? AssociatedSubtitles { get; init; }
}

internal sealed class CreateMediaSubtitlesBody
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("media_category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MediaCategory { get; init; }

    [JsonPropertyName("subtitles")]
    public required IReadOnlyCollection<MediaSubtitleBody> Subtitles { get; init; }
}

internal sealed class MediaSubtitleBody
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("language_code")]
    public required string LanguageCode { get; init; }

    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }
}

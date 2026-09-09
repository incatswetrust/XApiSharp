using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Media;

/// <summary>
/// Request for <c>POST /2/media/metadata</c>. <see cref="Metadata"/> is a raw
/// <see cref="JsonElement"/> escape hatch (SER-09/SER-12) - the registry's
/// "CreateMediaMetadataMetadata" schema branches into 12 independent nested shapes
/// (allow_download_status, alt_text, audience_policy, content_expiration,
/// domain_restrictions, found_media_origin, geo_restrictions, management_info, preview_image,
/// sensitive_media_warning, shared_info, sticker_info, upload_source) - too broad a surface to
/// model faithfully without a concrete consumer driving which parts matter; pass a
/// <see cref="JsonElement"/> shaped like that schema.
/// </summary>
public sealed class CreateMediaMetadataRequest
{
    public required string Id { get; init; }

    public JsonElement? Metadata { get; init; }
}

/// <summary>Modeled from the "CreateMediaMetadataResponse" schema.</summary>
public sealed class CreateMediaMetadataResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateMediaMetadataResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateMediaMetadataResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("associated_metadata")]
    public JsonElement? AssociatedMetadata { get; init; }
}

internal sealed class CreateMediaMetadataBody
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Metadata { get; init; }
}

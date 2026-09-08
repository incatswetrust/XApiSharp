using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>Modeled from the "Place" schema - embedded in <see cref="XIncludes.Places"/> via the
/// <c>geo.place_id</c> expansion. <c>geo</c> is a GeoJSON feature kept as
/// <see cref="JsonElement"/> (SER-09) rather than a bespoke GeoJSON type.</summary>
public sealed class Place
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; init; }

    [JsonPropertyName("place_type")]
    public string? PlaceType { get; init; }

    [JsonPropertyName("contained_within")]
    public IReadOnlyList<string>? ContainedWithin { get; init; }

    [JsonPropertyName("geo")]
    public JsonElement? Geo { get; init; }
}

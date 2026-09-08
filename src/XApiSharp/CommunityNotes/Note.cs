using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.CommunityNotes;

/// <summary>Modeled from the "Note" schema. <c>info</c>/<c>scoring_status</c>/<c>test_result</c>
/// stay <see cref="JsonElement"/> (SER-09) - nested, program-specific shapes low value to fully
/// type without a consumer driving the need.</summary>
public sealed class Note
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("info")]
    public JsonElement? Info { get; init; }

    [JsonPropertyName("scoring_status")]
    public JsonElement? ScoringStatus { get; init; }

    [JsonPropertyName("test_result")]
    public JsonElement? TestResult { get; init; }
}

using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>Modeled from the "Topic" schema - embedded in <see cref="XIncludes.Topics"/> via the
/// <c>topic_ids</c> expansion.</summary>
public sealed class Topic
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

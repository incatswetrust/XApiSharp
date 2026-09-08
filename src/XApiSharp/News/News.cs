using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.News;

/// <summary>Modeled from the "News" schema. <c>cluster_posts_results</c>/<c>contexts</c>/
/// <c>keywords</c> stay <see cref="JsonElement"/> (SER-09).</summary>
public sealed class News
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("hook")]
    public string? Hook { get; init; }

    [JsonPropertyName("disclaimer")]
    public string? Disclaimer { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("cluster_posts_results")]
    public JsonElement? ClusterPostsResults { get; init; }

    [JsonPropertyName("contexts")]
    public JsonElement? Contexts { get; init; }

    [JsonPropertyName("keywords")]
    public JsonElement? Keywords { get; init; }
}

using System.Text.Json.Serialization;

namespace XApiSharp.Communities;

/// <summary>Modeled from the "Community" schema.</summary>
public sealed class Community
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("access")]
    public string? Access { get; init; }

    [JsonPropertyName("join_policy")]
    public string? JoinPolicy { get; init; }

    [JsonPropertyName("member_count")]
    public int? MemberCount { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }
}

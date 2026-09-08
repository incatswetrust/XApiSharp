using System.Text.Json.Serialization;

namespace XApiSharp.Broadcasts;

/// <summary>Modeled from the "BroadcastChatMessage" schema.</summary>
public sealed class BroadcastChatMessage
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("broadcast_id")]
    public string? BroadcastId { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("author_id")]
    public string? AuthorId { get; init; }

    [JsonPropertyName("author_name")]
    public string? AuthorName { get; init; }

    [JsonPropertyName("author_username")]
    public string? AuthorUsername { get; init; }

    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; init; }

    [JsonPropertyName("created_at_ms")]
    public string? CreatedAtMs { get; init; }
}

using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>Modeled from the "Poll" schema - embedded in <see cref="XIncludes.Polls"/> via the
/// <c>attachments.poll_ids</c> expansion.</summary>
public sealed class Poll
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("options")]
    public IReadOnlyList<PollOption>? Options { get; init; }

    [JsonPropertyName("duration_minutes")]
    public int? DurationMinutes { get; init; }

    [JsonPropertyName("end_datetime")]
    public DateTimeOffset? EndDatetime { get; init; }

    [JsonPropertyName("voting_status")]
    public string? VotingStatus { get; init; }
}

public sealed class PollOption
{
    [JsonPropertyName("position")]
    public long Position { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("votes")]
    public long Votes { get; init; }
}

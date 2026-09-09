using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>Modeled from the "StreamPostsResponse" schema - one NDJSON line from the filtered
/// Post stream. The only streaming Post response with <see cref="MatchingRules"/> - the 7
/// volume-based Post streams (sample/sample10/firehose*) never populate it, hence the separate
/// <see cref="StreamPostResponse"/> for those.</summary>
public sealed class StreamPostsResponse
{
    [JsonPropertyName("data")]
    public Post? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("matching_rules")]
    public IReadOnlyList<StreamMatchingRule>? MatchingRules { get; init; }
}

public sealed class StreamMatchingRule
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }
}

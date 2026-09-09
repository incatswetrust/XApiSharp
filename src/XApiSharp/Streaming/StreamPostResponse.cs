using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>
/// One NDJSON line from any of the 7 volume-based Post streams (sample, sample10, firehose,
/// firehose-lang-en/ja/ko/pt) - each has its own distinctly-named schema in the registry
/// (<c>StreamPostsSampleResponse</c>, <c>StreamPostsFirehoseEnResponse</c>, etc.) but they are all
/// byte-for-byte the same shape (<c>{data, errors, includes}</c>, no <c>matching_rules</c>), so
/// one shared type serves all 7 rather than 7 near-identical classes.
/// </summary>
public sealed class StreamPostResponse
{
    [JsonPropertyName("data")]
    public Post? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }
}

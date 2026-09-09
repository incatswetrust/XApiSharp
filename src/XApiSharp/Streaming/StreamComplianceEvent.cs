using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>
/// One NDJSON line from any of the 4 compliance/label streams (Posts, Users, Likes compliance;
/// Post labels) - each declares a <c>oneOf {data} | {errors}</c> envelope around a genuinely
/// different discriminated union per operation (5 Post-compliance variants, 9 User-compliance
/// variants, 2 label variants, 1 Like-compliance variant). Modeling all 17 nested schema variants
/// isn't justified without a concrete consumer, so <see cref="Data"/> stays the SER-09/SER-12
/// "JsonElement escape hatch" - deserialize the specific variant you need from it once you know
/// which stream you're reading and what shape that stream's events take.
/// </summary>
public sealed class StreamComplianceEvent
{
    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }
}

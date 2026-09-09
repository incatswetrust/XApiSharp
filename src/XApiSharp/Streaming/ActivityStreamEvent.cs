using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>Modeled from the "ActivityStreamResponse" schema - one NDJSON line from
/// <c>GET /2/activity/stream</c>. Request shape is <see cref="ComplianceStreamRequest"/> (identical
/// <c>{backfill_minutes, start_time, end_time}</c>, no partition).</summary>
public sealed class ActivityStreamEvent
{
    [JsonPropertyName("data")]
    public ActivityStreamEventData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }
}

public sealed class ActivityStreamEventData
{
    /// <summary>e.g. <c>follow.follow</c>, <c>like.create</c>, <c>post.create</c> - the
    /// discriminator for <see cref="Payload"/>'s actual shape, per the registry's
    /// <c>ActivityStreamingResponsePayload</c> discriminated union.</summary>
    [JsonPropertyName("event_type")]
    public string? EventType { get; init; }

    [JsonPropertyName("event_uuid")]
    public string? EventUuid { get; init; }

    /// <summary>SER-09/SER-12 escape hatch - the subscription filter that matched, an
    /// implementation-specific shape not worth fully typing without a concrete consumer.</summary>
    [JsonPropertyName("filter")]
    public JsonElement? Filter { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    /// <summary>SER-09/SER-12 escape hatch - a discriminated union of 6 different payload shapes
    /// keyed by <see cref="EventType"/> (follow/news/like/Post-create/Post-delete/profile-update);
    /// deserialize the specific variant once you know which <see cref="EventType"/> arrived.</summary>
    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }
}

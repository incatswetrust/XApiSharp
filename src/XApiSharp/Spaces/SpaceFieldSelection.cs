using XApiSharp.Common;

namespace XApiSharp.Spaces;

/// <summary>Bundles the <c>space.fields</c>/<c>expansions</c>/<c>user.fields</c>/
/// <c>topic.fields</c> set every Spaces lookup operation shares identically.</summary>
public sealed class SpaceFieldSelection
{
    public IReadOnlyCollection<XSpaceField>? SpaceFields { get; init; }

    /// <summary>Only <c>creator_id</c>/<c>host_ids</c>/<c>invited_user_ids</c>/
    /// <c>speaker_ids</c>/<c>topic_ids</c> are meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }

    public IReadOnlyCollection<XTopicField>? TopicFields { get; init; }
}

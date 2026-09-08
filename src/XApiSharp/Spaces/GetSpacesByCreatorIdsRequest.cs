namespace XApiSharp.Spaces;

/// <summary>Request for <c>GET /2/spaces/by/creator_ids</c> - up to 100 user IDs per call, per the
/// registry.</summary>
public sealed class GetSpacesByCreatorIdsRequest
{
    public required IReadOnlyCollection<string> UserIds { get; init; }

    public SpaceFieldSelection? Fields { get; init; }
}

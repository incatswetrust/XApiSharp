namespace XApiSharp.Spaces;

/// <summary>Request for <c>GET /2/spaces</c> - up to 100 IDs per call, per the registry.</summary>
public sealed class GetSpacesByIdsRequest
{
    public required IReadOnlyCollection<string> Ids { get; init; }

    public SpaceFieldSelection? Fields { get; init; }
}

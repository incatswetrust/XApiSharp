namespace XApiSharp.Spaces;

/// <summary>Request for <c>GET /2/spaces/{id}</c>.</summary>
public sealed class GetSpaceRequest
{
    public required string Id { get; init; }

    public SpaceFieldSelection? Fields { get; init; }
}

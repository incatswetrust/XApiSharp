using XApiSharp.Common;

namespace XApiSharp.Spaces;

/// <summary>Request for <c>GET /2/spaces/search</c>.</summary>
public sealed class SearchSpacesRequest
{
    /// <summary>1-2048 characters, per the registry.</summary>
    public required string Query { get; init; }

    public XSpaceState? State { get; init; }

    public int? MaxResults { get; init; }

    public SpaceFieldSelection? Fields { get; init; }
}

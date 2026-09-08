using XApiSharp.Common;

namespace XApiSharp.Posts;

/// <summary>Request for <c>GET /2/tweets</c> - up to 100 IDs per call, per the registry.</summary>
public sealed class GetPostsByIdsRequest
{
    public required IReadOnlyCollection<string> Ids { get; init; }

    public XPostFieldSelection? Fields { get; init; }
}

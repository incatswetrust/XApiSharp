using XApiSharp.Common;

namespace XApiSharp.Posts;

/// <summary>Request for <c>GET /2/tweets/{id}</c>.</summary>
public sealed class GetPostRequest
{
    public required string Id { get; init; }

    public XPostFieldSelection? Fields { get; init; }
}

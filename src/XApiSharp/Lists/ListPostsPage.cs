using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Lists;

/// <summary>Request for <c>GET /2/lists/{id}/tweets</c>.</summary>
public sealed class ListPostsPageRequest
{
    public required string ListId { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public XPostFieldSelection? Fields { get; init; }
}

/// <summary>Modeled from the "GetListsPostsResponse" schema.</summary>
public sealed class ListPostsPageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Post>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

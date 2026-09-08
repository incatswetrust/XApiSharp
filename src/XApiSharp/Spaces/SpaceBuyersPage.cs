using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.Spaces;

/// <summary>Request for <c>GET /2/spaces/{id}/buyers</c> - pages over ticket-buying
/// <see cref="User"/>.</summary>
public sealed class SpaceBuyersPageRequest
{
    public required string SpaceId { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    /// <summary>Only <c>affiliation</c>/<c>most_recent_post_id</c>/<c>pinned_post_id</c> are
    /// meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

/// <summary>Modeled from the "GetSpacesBuyersResponse" schema.</summary>
public sealed class SpaceBuyersPageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<User>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

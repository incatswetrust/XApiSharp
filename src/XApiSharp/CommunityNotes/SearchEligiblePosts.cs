using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.CommunityNotes;

/// <summary>Request for <c>GET /2/notes/search/posts_eligible_for_notes</c>.</summary>
public sealed class SearchEligiblePostsRequest
{
    /// <summary>Required by the registry.</summary>
    public required bool TestMode { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    /// <summary>Raw passthrough for the registry's opaque <c>post_selection</c> string parameter -
    /// no documented enum of values to model as a closed type.</summary>
    public string? PostSelection { get; init; }

    public XPostFieldSelection? Fields { get; init; }
}

/// <summary>Modeled from the "SearchEligiblePostsResponse" schema.</summary>
public sealed class SearchEligiblePostsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Post>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public SearchNotesMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

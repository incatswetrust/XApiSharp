using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>
/// Request for <c>GET /2/users/{id}/bookmarks/folders</c>. Accepts <c>max_results</c>/
/// <c>pagination_token</c> per the registry, but the response schema declares no <c>meta</c> -
/// there's no documented way to know whether more folders exist beyond the current page, so this
/// stays a single-page-only operation (no <c>*PagesAsync</c>/<c>*Async</c> pagination overloads) -
/// spec 12.1's "don't promise a shape the contract doesn't provide" applies to pagination too.
/// </summary>
public sealed class GetBookmarkFoldersRequest
{
    public required string UserId { get; init; }

    public int? MaxResults { get; init; }

    public string? PaginationToken { get; init; }
}

/// <summary>Modeled from the "GetUsersBookmarkFoldersResponse" schema.</summary>
public sealed class GetBookmarkFoldersResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<BookmarkFolder>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class BookmarkFolder
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

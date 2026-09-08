using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>
/// Request for <c>GET /2/users/{id}/bookmarks/folders/{folder_id}</c>. Same no-documented-<c>meta</c>
/// situation as <see cref="GetBookmarkFoldersRequest"/> - single-page-only. Also notably returns
/// only Post IDs, not full <see cref="Common.Post"/> objects - no <c>post.fields</c>/
/// <c>expansions</c> support in the registry for this specific operation, unlike every other
/// bookmarks/posts-returning operation.
/// </summary>
public sealed class GetBookmarksByFolderRequest
{
    public required string UserId { get; init; }

    public required string FolderId { get; init; }

    public int? MaxResults { get; init; }

    public string? PaginationToken { get; init; }
}

/// <summary>Modeled from the "GetUsersBookmarksByFolderIdResponse" schema.</summary>
public sealed class GetBookmarksByFolderResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<BookmarkedPostId>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class BookmarkedPostId
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

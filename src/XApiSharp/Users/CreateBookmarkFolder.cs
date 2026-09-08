using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/bookmarks/folders</c>.</summary>
public sealed class CreateBookmarkFolderRequest
{
    public required string UserId { get; init; }

    /// <summary>1-25 characters, per the registry.</summary>
    public required string Name { get; init; }
}

/// <summary>Modeled from the "CreateUsersBookmarkFolderResponse" schema. Returned with HTTP 201,
/// not 200.</summary>
public sealed class CreateBookmarkFolderResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateBookmarkFolderResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateBookmarkFolderResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

internal sealed class CreateBookmarkFolderBody
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

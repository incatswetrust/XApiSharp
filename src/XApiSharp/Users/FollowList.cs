using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/followed_lists</c> - <see cref="UserId"/> follows
/// <see cref="ListId"/>.</summary>
public sealed class FollowListRequest
{
    public required string UserId { get; init; }

    public required string ListId { get; init; }
}

/// <summary>Modeled from the "FollowListResponse" schema.</summary>
public sealed class FollowListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public FollowListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class FollowListResponseData
{
    [JsonPropertyName("following")]
    public bool Following { get; init; }
}

internal sealed class FollowListBody
{
    [JsonPropertyName("list_id")]
    public required string ListId { get; init; }
}

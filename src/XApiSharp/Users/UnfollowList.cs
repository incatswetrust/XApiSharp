using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{id}/followed_lists/{list_id}</c>.</summary>
public sealed class UnfollowListRequest
{
    public required string UserId { get; init; }

    public required string ListId { get; init; }
}

/// <summary>Modeled from the "UnfollowListResponse" schema.</summary>
public sealed class UnfollowListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnfollowListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnfollowListResponseData
{
    [JsonPropertyName("following")]
    public bool Following { get; init; }
}

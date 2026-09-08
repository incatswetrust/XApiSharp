using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>DELETE /2/users/{id}/pinned_lists/{list_id}</c>.</summary>
public sealed class UnpinListRequest
{
    public required string UserId { get; init; }

    public required string ListId { get; init; }
}

/// <summary>Modeled from the "UnpinListResponse" schema.</summary>
public sealed class UnpinListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UnpinListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UnpinListResponseData
{
    [JsonPropertyName("pinned")]
    public bool Pinned { get; init; }
}

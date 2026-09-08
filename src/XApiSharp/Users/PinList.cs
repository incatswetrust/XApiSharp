using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Request for <c>POST /2/users/{id}/pinned_lists</c> - <see cref="UserId"/> pins
/// <see cref="ListId"/>.</summary>
public sealed class PinListRequest
{
    public required string UserId { get; init; }

    public required string ListId { get; init; }
}

/// <summary>Modeled from the "PinListResponse" schema.</summary>
public sealed class PinListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public PinListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class PinListResponseData
{
    [JsonPropertyName("pinned")]
    public bool Pinned { get; init; }
}

internal sealed class PinListBody
{
    [JsonPropertyName("list_id")]
    public required string ListId { get; init; }
}

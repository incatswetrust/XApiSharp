using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Lists;

/// <summary>Request for <c>DELETE /2/lists/{id}</c>.</summary>
public sealed class DeleteListRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "DeleteListsResponse" schema.</summary>
public sealed class DeleteListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteListResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Lists;

/// <summary>Request for <c>DELETE /2/lists/{id}/members/{user_id}</c>.</summary>
public sealed class RemoveListMemberRequest
{
    public required string ListId { get; init; }

    public required string UserId { get; init; }
}

/// <summary>Modeled from the "RemoveListsMemberByUserIdResponse" schema.</summary>
public sealed class RemoveListMemberResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ListMembershipResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

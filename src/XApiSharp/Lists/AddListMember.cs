using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Lists;

/// <summary>Request for <c>POST /2/lists/{id}/members</c>.</summary>
public sealed class AddListMemberRequest
{
    public required string ListId { get; init; }

    public required string UserId { get; init; }
}

/// <summary>Modeled from the "AddListsMemberResponse" schema.</summary>
public sealed class AddListMemberResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ListMembershipResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

/// <summary>Shared by <see cref="AddListMemberResponse"/> and
/// <see cref="RemoveListMemberResponse"/> - both are exactly <c>{"is_member": bool}</c> in the
/// registry.</summary>
public sealed class ListMembershipResponseData
{
    [JsonPropertyName("is_member")]
    public bool IsMember { get; init; }
}

internal sealed class AddListMemberBody
{
    [JsonPropertyName("user_id")]
    public required string UserId { get; init; }
}

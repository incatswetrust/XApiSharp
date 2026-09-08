using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Modeled from the "GetUsersByIdsResponse"/"GetUsersByUsernamesResponse" schemas - both
/// identical in shape, so shared by <see cref="UsersClient.GetByIdsAsync"/> and
/// <see cref="UsersClient.GetByUsernamesAsync"/>.</summary>
public sealed class GetUsersListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<User>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

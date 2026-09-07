using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Modeled from the "GetUsersByIdResponse" schema. <c>includes</c> (expansions) is
/// added alongside <c>fields</c>/<c>expansions</c> request support in E4.</summary>
public sealed class GetUserResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public User? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

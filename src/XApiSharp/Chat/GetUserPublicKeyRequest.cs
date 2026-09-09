using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>GET /2/users/{id}/public_keys</c> - single user, but the response is
/// still an array per the registry (a user can have keys for multiple devices/versions).</summary>
public sealed class GetUserPublicKeyRequest
{
    public required string UserId { get; init; }

    public IReadOnlyCollection<XPublicKeyField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetUsersPublicKeyResponse" schema.</summary>
public sealed class GetUserPublicKeyResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<PublicKey>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

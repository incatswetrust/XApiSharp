using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>GET /2/users/public_keys</c> - bulk lookup, up to 100 user IDs.</summary>
public sealed class GetUsersPublicKeysRequest
{
    public required IReadOnlyCollection<string> Ids { get; init; }

    public IReadOnlyCollection<XPublicKeyField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetUsersPublicKeysResponse" schema.</summary>
public sealed class GetUsersPublicKeysResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<PublicKey>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

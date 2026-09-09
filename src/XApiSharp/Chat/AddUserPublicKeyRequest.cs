using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>POST /2/users/{id}/public_keys</c> - registers identity/signing key
/// material for X Chat end-to-end encryption. Spec section 3.2: every key/signature field is
/// opaque, caller-produced (base64) material - the SDK never generates or validates it.</summary>
public sealed class AddUserPublicKeyRequest
{
    public required string UserId { get; init; }

    public required string PublicKey { get; init; }

    public required string Version { get; init; }

    public bool? GenerateVersion { get; init; }

    public string? IdentityPublicKeySignature { get; init; }

    public string? PublicKeyFingerprint { get; init; }

    public string? RegistrationMethod { get; init; }

    public string? SigningPublicKey { get; init; }

    public string? SigningPublicKeySignature { get; init; }
}

/// <summary>Modeled from the "AddUserPublicKeyResponse" schema.</summary>
public sealed class AddUserPublicKeyResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public AddUserPublicKeyResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class AddUserPublicKeyResponseData
{
    [JsonPropertyName("public_key_version")]
    public string? PublicKeyVersion { get; init; }

    /// <summary>SER-09/SER-12 escape hatch - an untyped recovery-service configuration object per
    /// the registry.</summary>
    [JsonPropertyName("juicebox_config")]
    public JsonElement? JuiceboxConfig { get; init; }
}

internal sealed class AddUserPublicKeyBody
{
    [JsonPropertyName("public_key")]
    public required AddUserPublicKeyPublicKeyBody PublicKey { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("generate_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GenerateVersion { get; init; }
}

internal sealed class AddUserPublicKeyPublicKeyBody
{
    [JsonPropertyName("public_key")]
    public required string PublicKey { get; init; }

    [JsonPropertyName("identity_public_key_signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdentityPublicKeySignature { get; init; }

    [JsonPropertyName("public_key_fingerprint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PublicKeyFingerprint { get; init; }

    [JsonPropertyName("registration_method")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistrationMethod { get; init; }

    [JsonPropertyName("signing_public_key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SigningPublicKey { get; init; }

    [JsonPropertyName("signing_public_key_signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SigningPublicKeySignature { get; init; }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Chat;

/// <summary>Modeled from the "PublicKey" schema - shared by <c>getUsersPublicKeys</c> and
/// <c>getUsersPublicKey</c>. Spec section 3.2: key material stays opaque, plain strings.</summary>
public sealed class PublicKey
{
    [JsonPropertyName("public_key")]
    public string? Value { get; init; }

    [JsonPropertyName("public_key_version")]
    public string? PublicKeyVersion { get; init; }

    [JsonPropertyName("identity_public_key_signature")]
    public string? IdentityPublicKeySignature { get; init; }

    [JsonPropertyName("signing_public_key")]
    public string? SigningPublicKey { get; init; }

    /// <summary>SER-09/SER-12 escape hatch - an untyped recovery-service configuration object per
    /// the registry.</summary>
    [JsonPropertyName("juicebox_config")]
    public JsonElement? JuiceboxConfig { get; init; }
}

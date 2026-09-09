namespace XApiSharp.Common;

/// <summary>The <c>public_key.fields</c> query parameter (SER-05).</summary>
public enum XPublicKeyField
{
    IdentityPublicKeySignature,
    JuiceboxConfig,
    PublicKey,
    PublicKeyVersion,
    SigningPublicKey,
}

public static class XPublicKeyFieldExtensions
{
    public static string ToApiValue(this XPublicKeyField field) => field switch
    {
        XPublicKeyField.IdentityPublicKeySignature => "identity_public_key_signature",
        XPublicKeyField.JuiceboxConfig => "juicebox_config",
        XPublicKeyField.PublicKey => "public_key",
        XPublicKeyField.PublicKeyVersion => "public_key_version",
        XPublicKeyField.SigningPublicKey => "signing_public_key",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

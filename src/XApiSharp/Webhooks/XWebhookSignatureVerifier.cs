using System.Security.Cryptography;
using System.Text;

namespace XApiSharp.Webhooks;

/// <summary>
/// Verifies the <c>x-twitter-webhooks-signature</c> header X sends on every delivered webhook
/// event - a small, independent helper (spec 17.1), with no dependency on <see cref="WebhooksClient"/>.
/// Verified against current docs (docs.x.com/x-api/webhooks/quickstart, 2026-09-09): the header
/// value is <c>sha256=&lt;base64 HMAC-SHA256(consumer secret, raw request body bytes)&gt;</c>.
/// Comparison is constant-time (spec 17.1: "сравнивать подписи за постоянное время") via
/// <see cref="CryptographicOperations.FixedTimeEquals"/>, so a timing side-channel can't leak how
/// many leading bytes of a guessed signature matched.
/// </summary>
public static class XWebhookSignatureVerifier
{
    private const string Prefix = "sha256=";

    /// <summary>
    /// Verifies <paramref name="signatureHeaderValue"/> (the raw <c>x-twitter-webhooks-signature</c>
    /// header value, <c>sha256=</c> prefix included) against <paramref name="rawBody"/> - the
    /// exact, unmodified request body bytes (spec 17.1: "проверять подпись на исходных байтах
    /// тела"). Re-parsing/re-serializing the body before calling this changes the bytes and will
    /// make a genuine signature fail to verify.
    /// </summary>
    public static bool Verify(ReadOnlySpan<byte> rawBody, string signatureHeaderValue, string consumerSecret)
    {
        ArgumentException.ThrowIfNullOrEmpty(consumerSecret);

        if (signatureHeaderValue is null || !signatureHeaderValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var providedBase64 = signatureHeaderValue.AsSpan(Prefix.Length);
        Span<byte> provided = stackalloc byte[64];
        if (!Convert.TryFromBase64Chars(providedBase64, provided, out var providedLength))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(consumerSecret));
        Span<byte> expected = stackalloc byte[32];
        hmac.TryComputeHash(rawBody, expected, out var expectedLength);

        return CryptographicOperations.FixedTimeEquals(provided[..providedLength], expected[..expectedLength]);
    }
}

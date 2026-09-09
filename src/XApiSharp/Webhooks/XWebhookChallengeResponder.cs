using System.Security.Cryptography;
using System.Text;

namespace XApiSharp.Webhooks;

/// <summary>
/// Answers the CRC ("Challenge-Response Check") request X sends to a registered webhook URL, both
/// at registration time and whenever <see cref="WebhooksClient.ValidateAsync"/> re-triggers one -
/// a small, independent helper with no dependency on <see cref="WebhooksClient"/>/<c>HttpClient</c>
/// (spec 17.1: "небольшие независимые helpers"), since this runs in the app's own inbound
/// endpoint, not as an outgoing SDK call. Verified against current docs (docs.x.com/x-api/webhooks/quickstart,
/// 2026-09-09), not carried over from the legacy v1.1 scheme unverified (spec 17.1): X sends a GET
/// with a <c>crc_token</c> query parameter and expects back
/// <c>{"response_token": "sha256=&lt;base64 HMAC-SHA256(consumer secret, crc_token)&gt;"}</c>.
/// </summary>
public static class XWebhookChallengeResponder
{
    /// <summary>Computes the <c>response_token</c> value (including the <c>sha256=</c> prefix)
    /// for a given <c>crc_token</c> query parameter value.</summary>
    /// <param name="crcToken">The raw, un-decoded <c>crc_token</c> query parameter value from the
    /// incoming CRC request.</param>
    /// <param name="consumerSecret">The app's consumer secret (API secret key) - the same value
    /// used to sign the request.</param>
    public static string ComputeResponseToken(string crcToken, string consumerSecret)
    {
        ArgumentException.ThrowIfNullOrEmpty(crcToken);
        ArgumentException.ThrowIfNullOrEmpty(consumerSecret);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(consumerSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(crcToken));
        return "sha256=" + Convert.ToBase64String(hash);
    }

    /// <summary>
    /// The full JSON response body to send back for the CRC request, as raw UTF-8 bytes -
    /// <c>{"response_token":"sha256=..."}</c>, matching the exact field name and format the
    /// registry's CRC handshake requires. Callers using a JSON-serializing web framework can write
    /// <c>new { response_token = ComputeResponseToken(...) }</c> directly instead; this overload is
    /// for a raw/minimal-API response where writing the bytes yourself is more convenient.
    /// </summary>
    public static byte[] BuildResponseBody(string crcToken, string consumerSecret)
    {
        var responseToken = ComputeResponseToken(crcToken, consumerSecret);
        return Encoding.UTF8.GetBytes($"{{\"response_token\":\"{responseToken}\"}}");
    }
}

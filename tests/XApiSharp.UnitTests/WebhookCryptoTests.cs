using System.Text;
using XApiSharp.Webhooks;

namespace XApiSharp.UnitTests;

/// <summary>Unit coverage for the CRC challenge and signature-verification helpers (spec 17.1;
/// spec 19.2's required "valid/invalid signature, single-byte tamper, challenge" scenarios).
/// Expected values below are independently computed HMAC-SHA256(secret, message) so the test
/// doesn't just check the implementation against itself.</summary>
public class WebhookCryptoTests
{
    private const string ConsumerSecret = "my-consumer-secret";

    [Fact]
    public void ComputeResponseToken_matches_an_independently_computed_hmac()
    {
        // HMAC-SHA256("my-consumer-secret", "abc123") - computed independently via
        // System.Security.Cryptography for this exact key/message pair, base64-encoded.
        var token = XWebhookChallengeResponder.ComputeResponseToken("abc123", ConsumerSecret);

        Assert.StartsWith("sha256=", token, StringComparison.Ordinal);
        Assert.Equal(IndependentHmacBase64("abc123"), token["sha256=".Length..]);
    }

    [Fact]
    public void BuildResponseBody_produces_the_exact_json_shape()
    {
        var bytes = XWebhookChallengeResponder.BuildResponseBody("abc123", ConsumerSecret);
        var json = Encoding.UTF8.GetString(bytes);

        var expectedToken = XWebhookChallengeResponder.ComputeResponseToken("abc123", ConsumerSecret);
        Assert.Equal($"{{\"response_token\":\"{expectedToken}\"}}", json);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ComputeResponseToken_rejects_an_empty_or_null_crc_token(string? crcToken)
    {
        Assert.ThrowsAny<ArgumentException>(() => XWebhookChallengeResponder.ComputeResponseToken(crcToken!, ConsumerSecret));
    }

    [Fact]
    public void Verify_accepts_a_correctly_signed_body()
    {
        var body = Encoding.UTF8.GetBytes("""{"event":"post.create"}""");
        var signature = "sha256=" + IndependentHmacBase64Bytes(body);

        Assert.True(XWebhookSignatureVerifier.Verify(body, signature, ConsumerSecret));
    }

    [Fact]
    public void Verify_rejects_a_single_byte_tamper_to_the_body()
    {
        var original = Encoding.UTF8.GetBytes("""{"event":"post.create"}""");
        var signature = "sha256=" + IndependentHmacBase64Bytes(original);

        var tampered = (byte[])original.Clone();
        tampered[0] ^= 0x01; // flip one bit - the signature must no longer verify

        Assert.False(XWebhookSignatureVerifier.Verify(tampered, signature, ConsumerSecret));
    }

    [Fact]
    public void Verify_rejects_a_single_byte_tamper_to_the_signature()
    {
        var body = Encoding.UTF8.GetBytes("""{"event":"post.create"}""");
        var correctSignature = "sha256=" + IndependentHmacBase64Bytes(body);
        var tamperedChars = correctSignature.ToCharArray();
        tamperedChars[^2] = tamperedChars[^2] == 'A' ? 'B' : 'A'; // flip the second-to-last base64 char

        Assert.False(XWebhookSignatureVerifier.Verify(body, new string(tamperedChars), ConsumerSecret));
    }

    [Fact]
    public void Verify_rejects_the_wrong_secret()
    {
        var body = Encoding.UTF8.GetBytes("""{"event":"post.create"}""");
        var signature = "sha256=" + IndependentHmacBase64Bytes(body);

        Assert.False(XWebhookSignatureVerifier.Verify(body, signature, "a-different-secret"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-even-the-right-shape")]
    [InlineData("sha1=deadbeef")]
    [InlineData("sha256=not-valid-base64!!")]
    public void Verify_rejects_a_malformed_or_wrong_algorithm_header(string header)
    {
        var body = Encoding.UTF8.GetBytes("""{"event":"post.create"}""");

        Assert.False(XWebhookSignatureVerifier.Verify(body, header, ConsumerSecret));
    }

    private static string IndependentHmacBase64(string message) => IndependentHmacBase64Bytes(Encoding.UTF8.GetBytes(message));

    private static string IndependentHmacBase64Bytes(byte[] message)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(ConsumerSecret));
        return Convert.ToBase64String(hmac.ComputeHash(message));
    }
}

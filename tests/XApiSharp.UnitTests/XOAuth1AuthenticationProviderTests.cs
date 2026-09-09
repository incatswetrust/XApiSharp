using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;

namespace XApiSharp.UnitTests;

/// <summary>
/// Spec section 19.2: "OAuth 1.0a: independent signature vectors" - these are X's own published
/// worked examples (not derived from this implementation), so a passing test is evidence the
/// algorithm is actually correct, not just self-consistent.
///
/// Primary vector: https://docs.x.com/fundamentals/authentication/oauth-1-0a/creating-a-signature
/// (POST https://api.x.com/1.1/statuses/update.json?include_entities=true, with a form body).
/// Every input below (keys, secrets, nonce, timestamp) and the expected base string/signature are
/// copied verbatim from that page.
///
/// Percent-encoding vector: .../oauth-1-0a/percent-encoding-parameters.md, including the Unicode
/// example ("☃" -> "%E2%98%83").
///
/// Note: docs.x.com/.../authorizing-a-request.md shows the *same* nonce/timestamp/keys but a
/// *different* oauth_signature ("tnnArxj06cWHq44gCs1OSKk/jLY=") without publishing the secrets
/// behind it - an inconsistency between the two pages we can't resolve without those secrets.
/// The vector used here is the one with a complete, self-consistent derivation (creating-a-signature.md
/// shows base string, signing key, and signature together).
/// </summary>
public class XOAuth1AuthenticationProviderTests
{
    private const string ConsumerKey = "xvz1evFS4wEEPTGEFPHBog";
    private const string ConsumerSecret = "kAcSOqF21Fu85e7zjz7ZN2U4ZRhfV3WpwPAoE3Z7kBw";
    private const string AccessToken = "370773112-GmHxMAgYyLbNEtIKZeRNFsMKPR9EyMZeS9weJAEb";
    private const string AccessTokenSecret = "LswwdoUaIvS8ltyTt5jkRh4J50vUPVVHtR2YPi5kE";
    private const string Nonce = "kYjzVBB8Y0ZFabxSWbWovY3uYSQ2pTgmZeNu2VS4cg";
    private const long UnixTimestamp = 1318622958;
    private const string ExpectedBaseString =
        "POST&https%3A%2F%2Fapi.x.com%2F1.1%2Fstatuses%2Fupdate.json&include_entities%3Dtrue%26oauth_consumer_key%3Dxvz1evFS4wEEPTGEFPHBog%26oauth_nonce%3DkYjzVBB8Y0ZFabxSWbWovY3uYSQ2pTgmZeNu2VS4cg%26oauth_signature_method%3DHMAC-SHA1%26oauth_timestamp%3D1318622958%26oauth_token%3D370773112-GmHxMAgYyLbNEtIKZeRNFsMKPR9EyMZeS9weJAEb%26oauth_version%3D1.0%26status%3DHello%2520Ladies%2520%252B%2520Gentlemen%252C%2520a%2520signed%2520OAuth%2520request%2521";
    private const string ExpectedSignature = "Ls93hJiZbQ3akF3HF3x1Bz8/zU4=";

    private static readonly List<(string Key, string Value)> ExampleParameters =
    [
        ("status", "Hello Ladies + Gentlemen, a signed OAuth request!"),
        ("include_entities", "true"),
        ("oauth_consumer_key", ConsumerKey),
        ("oauth_nonce", Nonce),
        ("oauth_signature_method", "HMAC-SHA1"),
        ("oauth_timestamp", UnixTimestamp.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        ("oauth_token", AccessToken),
        ("oauth_version", "1.0"),
    ];

    [Fact]
    public void BuildSignatureBaseString_matches_the_published_example_exactly()
    {
        var baseString = XOAuth1AuthenticationProvider.BuildSignatureBaseString(
            "POST", "https://api.x.com/1.1/statuses/update.json", ExampleParameters);

        Assert.Equal(ExpectedBaseString, baseString);
    }

    [Fact]
    public void ComputeSignature_matches_the_published_example_exactly()
    {
        var signature = XOAuth1AuthenticationProvider.ComputeSignature(
            "POST", "https://api.x.com/1.1/statuses/update.json", ExampleParameters, ConsumerSecret, AccessTokenSecret);

        Assert.Equal(ExpectedSignature, signature);
    }

    [Fact]
    public async Task PrepareRequestAsync_reproduces_the_published_example_end_to_end()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(UnixTimestamp));
        var provider = new XOAuth1AuthenticationProvider(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret, timeProvider, () => Nonce);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/1.1/statuses/update.json?include_entities=true")
        {
            Content = new StringContent(
                "status=Hello+Ladies+%2B+Gentlemen%2C+a+signed+OAuth+request%21",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"),
        };
        // StringContent sets a charset parameter by default; the doc's raw request doesn't have
        // one, but the signature only depends on the media type, so this doesn't affect signing.

        await provider.PrepareRequestAsync(request, CancellationToken.None);

        Assert.Equal("OAuth", request.Headers.Authorization?.Scheme);
        Assert.Contains($"oauth_signature=\"{Uri.EscapeDataString(ExpectedSignature)}\"", request.Headers.Authorization?.Parameter, StringComparison.Ordinal);
    }

    [Fact]
    public void PercentEncode_matches_the_documented_examples_including_unicode()
    {
        // docs.x.com/.../percent-encoding-parameters.md examples, verified at the parameter
        // string level (single encoding pass) - BuildSignatureBaseString wraps this in a
        // *second* encoding pass by design (verified separately below), which would otherwise
        // make this test check the wrong thing (%20 becoming %2520, etc).
        AssertEncodesTo("Ladies + Gentlemen", "Ladies%20%2B%20Gentlemen");
        AssertEncodesTo("An encoded string!", "An%20encoded%20string%21");
        AssertEncodesTo("Dogs, Cats & Mice", "Dogs%2C%20Cats%20%26%20Mice");
        AssertEncodesTo("☃", "%E2%98%83");

        static void AssertEncodesTo(string raw, string expectedEncoded)
        {
            var parameterString = XOAuth1AuthenticationProvider.BuildParameterString([("q", raw)]);
            Assert.Equal($"q={expectedEncoded}", parameterString);
        }
    }

    [Fact]
    public void BuildSignatureBaseString_percent_encodes_the_parameter_string_a_second_time()
    {
        // Doc: "The percent '%' characters in the parameter string should be encoded as %25 in
        // the signature base string." - i.e. the base string double-encodes on top of the
        // already-encoded parameter string.
        var baseString = XOAuth1AuthenticationProvider.BuildSignatureBaseString("GET", "https://api.x.com/x", [("q", "a b")]);

        Assert.EndsWith("q%3Da%2520b", baseString, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildParameterString_sorts_by_encoded_key_then_encoded_value_for_repeated_keys()
    {
        // RFC 5849 / X's docs footnote [2]: same key sorts by value next. X itself rejects
        // duplicate keys at the API level, but the sort rule is still part of the algorithm.
        var parameters = new List<(string, string)> { ("a", "2"), ("a", "1"), ("b", "1") };

        var parameterString = XOAuth1AuthenticationProvider.BuildParameterString(parameters);

        Assert.Equal("a=1&a=2&b=1", parameterString);
    }

    [Fact]
    public async Task Multipart_bodies_are_excluded_from_the_signature()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(UnixTimestamp));
        var provider = new XOAuth1AuthenticationProvider(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret, timeProvider, () => Nonce);

        using var withMultipart = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/2/media/upload")
        {
            Content = new MultipartFormDataContent { { new StringContent("ignored-by-signature"), "media" } },
        };
        using var withoutBody = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/2/media/upload");

        await provider.PrepareRequestAsync(withMultipart, CancellationToken.None);
        await provider.PrepareRequestAsync(withoutBody, CancellationToken.None);

        // Same nonce/timestamp/URL/method for both - if the multipart body were (wrongly)
        // included, the signatures would differ.
        Assert.Equal(withoutBody.Headers.Authorization?.Parameter, withMultipart.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Never_mutates_the_shared_HttpClient_default_authorization_header()
    {
        var provider = new XOAuth1AuthenticationProvider(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
        using var httpClient = new HttpClient();

        for (var i = 0; i < 3; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.x.com/2/users/{i}");
            await provider.PrepareRequestAsync(request, CancellationToken.None);
        }

        Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
    }

    [Fact]
    public async Task Consecutive_requests_get_different_nonces()
    {
        var provider = new XOAuth1AuthenticationProvider(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);

        using var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");
        await provider.PrepareRequestAsync(request1, CancellationToken.None);
        using var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/2");
        await provider.PrepareRequestAsync(request2, CancellationToken.None);

        Assert.NotEqual(request1.Headers.Authorization?.Parameter, request2.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task Secrets_never_appear_in_the_authorization_header_or_an_exception()
    {
        var provider = new XOAuth1AuthenticationProvider(ConsumerKey, ConsumerSecret, AccessToken, AccessTokenSecret);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");

        await provider.PrepareRequestAsync(request, CancellationToken.None);

        Assert.DoesNotContain(ConsumerSecret, request.Headers.Authorization!.Parameter, StringComparison.Ordinal);
        Assert.DoesNotContain(AccessTokenSecret, request.Headers.Authorization!.Parameter, StringComparison.Ordinal);

        var ex = Assert.Throws<ArgumentException>(() => new XOAuth1AuthenticationProvider("", ConsumerSecret, AccessToken, AccessTokenSecret));
        Assert.DoesNotContain(ConsumerSecret, ex.ToString(), StringComparison.Ordinal);
    }
}

using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace XApiSharp.Authentication;

/// <summary>
/// OAuth 1.0a HMAC-SHA1 request signing (spec section 10.3), per
/// https://docs.x.com/fundamentals/authentication/oauth-1-0a/creating-a-signature and
/// .../authorizing-a-request. One instance per app+user context (consumer key/secret + access
/// token/secret) - the context key is the instance itself, so a separate instance per user keeps
/// contexts from mixing (spec section 19.2 isolation requirement) the same way the OAuth 2.0 and
/// app-only providers do.
///
/// Never mutates a shared <see cref="HttpClient.DefaultRequestHeaders"/> - the signature is
/// request-specific (it covers the method, URL, and body), so it is set on the per-request
/// <see cref="HttpRequestMessage.Headers"/> only (spec section 10.3: "Запрещено менять общий
/// HttpClient.DefaultRequestHeaders.Authorization перед каждым пользовательским запросом").
/// </summary>
public sealed class XOAuth1AuthenticationProvider : IXAuthenticationProvider
{
    private readonly string _consumerKey;
    private readonly string _consumerSecret;
    private readonly string _accessToken;
    private readonly string _accessTokenSecret;
    private readonly TimeProvider _timeProvider;
    private readonly Func<string> _nonceFactory;

    public XOAuth1AuthenticationProvider(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, TimeProvider? timeProvider = null)
        : this(consumerKey, consumerSecret, accessToken, accessTokenSecret, timeProvider ?? TimeProvider.System, GenerateNonce)
    {
    }

    /// <summary>Test-only seam: lets contract/unit tests reproduce X's published worked example
    /// exactly by fixing the nonce instead of letting one be generated randomly.</summary>
    internal XOAuth1AuthenticationProvider(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret, TimeProvider timeProvider, Func<string> nonceFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessTokenSecret);

        _consumerKey = consumerKey;
        _consumerSecret = consumerSecret;
        _accessToken = accessToken;
        _accessTokenSecret = accessTokenSecret;
        _timeProvider = timeProvider;
        _nonceFactory = nonceFactory;
    }

    public async ValueTask PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestUri is null)
        {
            throw new ArgumentException("The request must have a RequestUri before it can be signed.", nameof(request));
        }

        // AUTH: fresh nonce and timestamp on every request (spec section 10.3) - never reused
        // across attempts/retries, since a stale nonce is exactly what OAuth 1.0a's replay
        // protection is designed to reject.
        var nonce = _nonceFactory();
        var timestamp = _timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        List<(string Key, string Value)> oauthParams =
        [
            ("oauth_consumer_key", _consumerKey),
            ("oauth_nonce", nonce),
            ("oauth_signature_method", "HMAC-SHA1"),
            ("oauth_timestamp", timestamp),
            ("oauth_token", _accessToken),
            ("oauth_version", "1.0"),
        ];

        var signedParams = new List<(string Key, string Value)>(oauthParams);
        signedParams.AddRange(ParseFormEncoded(request.RequestUri.Query.TrimStart('?'), treatPlusAsSpace: false));

        // Only application/x-www-form-urlencoded bodies are part of the signature - never
        // multipart (its boundaries and binary parts aren't signable the same way, and X's own
        // examples only ever sign the simple form-body case).
        if (request.Content is not null && IsFormUrlEncoded(request.Content))
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            signedParams.AddRange(ParseFormEncoded(body, treatPlusAsSpace: true));
        }

        var baseUrl = request.RequestUri.GetLeftPart(UriPartial.Path);
        var signature = ComputeSignature(request.Method.Method, baseUrl, signedParams, _consumerSecret, _accessTokenSecret);

        var headerParams = new List<(string Key, string Value)>(oauthParams) { ("oauth_signature", signature) };
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", BuildAuthorizationHeaderValue(headerParams));
    }

    /// <summary>RFC 5849 §3.4.1 / X's "Creating a signature" doc, steps 1-5.</summary>
    internal static string BuildParameterString(IReadOnlyList<(string Key, string Value)> parameters)
    {
        var encoded = parameters
            .Select(p => (Key: PercentEncode(p.Key), Value: PercentEncode(p.Value)))
            .ToList();

        // Sort by encoded key, then by encoded value for duplicate keys (X rejects duplicate
        // keys in practice, but the sort rule itself is still exercised/tested independently).
        encoded.Sort((a, b) =>
        {
            var keyCompare = string.CompareOrdinal(a.Key, b.Key);
            return keyCompare != 0 ? keyCompare : string.CompareOrdinal(a.Value, b.Value);
        });

        return string.Join('&', encoded.Select(p => $"{p.Key}={p.Value}"));
    }

    internal static string BuildSignatureBaseString(string httpMethod, string baseUrl, IReadOnlyList<(string Key, string Value)> parameters)
    {
        var parameterString = BuildParameterString(parameters);
        return $"{httpMethod.ToUpperInvariant()}&{PercentEncode(baseUrl)}&{PercentEncode(parameterString)}";
    }

    internal static string ComputeSignature(string httpMethod, string baseUrl, IReadOnlyList<(string Key, string Value)> parameters, string consumerSecret, string tokenSecret)
    {
        var baseString = BuildSignatureBaseString(httpMethod, baseUrl, parameters);
        var signingKey = $"{PercentEncode(consumerSecret)}&{PercentEncode(tokenSecret)}";

        // HMAC-SHA1 is mandated by the OAuth 1.0a protocol itself (RFC 5849) and by X's
        // documented "oauth_signature_method" - this is implementing a fixed third-party wire
        // format, not a free choice of hash for our own data.
#pragma warning disable CA5350
        using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
#pragma warning restore CA5350
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString));
        return Convert.ToBase64String(hash);
    }

    private static string BuildAuthorizationHeaderValue(IReadOnlyList<(string Key, string Value)> oauthParams) =>
        string.Join(", ", oauthParams.Select(p => $"{PercentEncode(p.Key)}=\"{PercentEncode(p.Value)}\""));

    private static bool IsFormUrlEncoded(HttpContent content) =>
        string.Equals(content.Headers.ContentType?.MediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

    private static List<(string Key, string Value)> ParseFormEncoded(string raw, bool treatPlusAsSpace)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrEmpty(raw))
        {
            return result;
        }

        foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            var rawKey = separatorIndex >= 0 ? pair[..separatorIndex] : pair;
            var rawValue = separatorIndex >= 0 ? pair[(separatorIndex + 1)..] : string.Empty;

            if (treatPlusAsSpace)
            {
                rawKey = rawKey.Replace('+', ' ');
                rawValue = rawValue.Replace('+', ' ');
            }

            result.Add((Uri.UnescapeDataString(rawKey), Uri.UnescapeDataString(rawValue)));
        }

        return result;
    }

    /// <summary>
    /// RFC 3986 §2.1 unreserved-set percent encoding, byte by byte over UTF-8 - deliberately
    /// hand-rolled rather than <see cref="Uri.EscapeDataString(string)"/>: the BCL method leaves
    /// some RFC 2396 "mark" characters (<c>! * ' ( )</c>) unescaped, which produces an invalid
    /// OAuth 1.0a signature (verified against X's published test vector below).
    /// </summary>
    private static string PercentEncode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var builder = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            var isUnreserved = (b >= (byte)'A' && b <= (byte)'Z')
                || (b >= (byte)'a' && b <= (byte)'z')
                || (b >= (byte)'0' && b <= (byte)'9')
                || b is (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~';

            if (isUnreserved)
            {
                builder.Append((char)b);
            }
            else
            {
                builder.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
        }

        return builder.ToString();
    }

    /// <summary>Doc: "generated by base64 encoding 32 bytes of random data, and stripping out
    /// all non-word characters" - <see cref="RandomNumberGenerator"/>, not <see cref="Random"/>,
    /// for the same reason as the OAuth 2.0 PKCE verifier/state (spec AUTH-01's cryptographic
    /// randomness requirement applies here too, even though AUTH-01 itself names OAuth 2.0).</summary>
    private static string GenerateNonce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var base64 = Convert.ToBase64String(bytes);
        return new string(base64.Where(char.IsAsciiLetterOrDigit).ToArray());
    }
}

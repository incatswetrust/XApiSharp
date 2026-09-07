using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;
using XApiSharp.Transport;

namespace XApiSharp.Authentication;

/// <summary>
/// OAuth 2.0 Authorization Code flow with PKCE (spec section 10.2), per
/// https://docs.x.com/fundamentals/authentication/oauth-2-0/authorization-code. Builds the
/// authorize URL and exchanges the returned code for tokens - it never opens a browser or starts
/// a listener itself (API-09). Refresh (AUTH-06..11) lands in a later E3 commit; this covers the
/// initial exchange plus the shared token-request plumbing refresh will reuse.
/// </summary>
public sealed class XOAuth2Client
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Uri AuthorizeUrl = new("https://x.com/i/oauth2/authorize");

    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private readonly Uri _tokenEndpoint;
    private readonly TimeProvider _timeProvider;

    /// <param name="httpClient">Externally owned - reused as-is, never disposed (HTTP-01).</param>
    /// <param name="clientId">The app's OAuth 2.0 Client ID.</param>
    /// <param name="clientSecret">Omit for a public client (native app, single-page app);
    /// provide for a confidential client (web app, automated app/bot) - spec AUTH-04. Determines
    /// whether the client authenticates via an Authorization header (confidential) or a
    /// client_id body field (public) on every token request.</param>
    /// <param name="apiBaseUrl">Defaults to <c>https://api.x.com</c>; override for tests.</param>
    /// <param name="timeProvider">Defaults to <see cref="TimeProvider.System"/>.</param>
    public XOAuth2Client(HttpClient httpClient, string clientId, string? clientSecret = null, Uri? apiBaseUrl = null, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = new Uri(apiBaseUrl ?? new Uri("https://api.x.com"), "2/oauth2/token");
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Starts a flow: generates PKCE (S256, cryptographically random verifier - AUTH-01) and
    /// <c>state</c> (AUTH-02), and builds the URL to send the user's browser to. The caller
    /// requests exactly the scopes it needs (AUTH-05) - <c>offline.access</c> is not added
    /// automatically; include it explicitly to receive a refresh token.
    /// </summary>
    public XOAuth2AuthorizationRequest CreateAuthorizationRequest(Uri redirectUri, IReadOnlyList<string> scopes, TimeSpan? sessionLifetime = null)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);
        if (!redirectUri.IsAbsoluteUri)
        {
            throw new ArgumentException("redirectUri must be absolute.", nameof(redirectUri));
        }

        var codeVerifier = GeneratePkceCodeVerifier();
        var codeChallenge = ComputeS256Challenge(codeVerifier);
        var state = GenerateRandomUrlSafeToken(32);
        var expiresAtUtc = _timeProvider.GetUtcNow() + (sessionLifetime ?? TimeSpan.FromMinutes(10));

        var query = QueryStringBuilder.Build(
        [
            ("response_type", "code"),
            ("client_id", _clientId),
            ("redirect_uri", redirectUri.ToString()),
            ("scope", string.Join(' ', scopes)),
            ("state", state),
            ("code_challenge", codeChallenge),
            ("code_challenge_method", "S256"),
        ]);

        return new XOAuth2AuthorizationRequest
        {
            AuthorizationUrl = new Uri($"{AuthorizeUrl}{query}"),
            State = state,
            CodeVerifier = codeVerifier,
            RedirectUri = redirectUri,
            ExpiresAtUtc = expiresAtUtc,
        };
    }

    /// <summary>
    /// Validates the browser's callback against <paramref name="request"/> (session not expired,
    /// no OAuth error, <c>state</c> matches, callback origin/path matches the original
    /// <c>redirect_uri</c> - AUTH-02/AUTH-03) and, if valid, exchanges the code for tokens.
    /// </summary>
    public async Task<XOAuth2TokenResponse> CompleteAuthorizationAsync(XOAuth2AuthorizationRequest request, Uri callbackUri, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(callbackUri);

        if (_timeProvider.GetUtcNow() > request.ExpiresAtUtc)
        {
            throw new XAuthenticationException("The authorization session has expired; restart the authorization flow.");
        }

        if (!string.Equals(
            callbackUri.GetLeftPart(UriPartial.Path),
            request.RedirectUri.GetLeftPart(UriPartial.Path),
            StringComparison.Ordinal))
        {
            throw new XAuthenticationException("The callback URI does not match the redirect_uri this authorization session was started with.");
        }

        var callbackParams = ParseQueryString(callbackUri.Query);

        if (callbackParams.TryGetValue("error", out var error))
        {
            var description = callbackParams.GetValueOrDefault("error_description");
            throw new XAuthenticationException(
                description is null ? $"Authorization failed: {error}." : $"Authorization failed: {error} ({description}).");
        }

        if (!callbackParams.TryGetValue("state", out var returnedState) || !string.Equals(returnedState, request.State, StringComparison.Ordinal))
        {
            throw new XAuthenticationException("The callback 'state' parameter did not match this authorization session (possible CSRF or stale session).");
        }

        if (!callbackParams.TryGetValue("code", out var code) || string.IsNullOrEmpty(code))
        {
            throw new XAuthenticationException("The callback did not include an authorization code.");
        }

        return await ExchangeCodeAsync(code, request.CodeVerifier, request.RedirectUri, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Lower-level exchange, for callers that parse/validate the callback themselves.
    /// Prefer <see cref="CompleteAuthorizationAsync"/> for the full, validated flow.</summary>
    public Task<XOAuth2TokenResponse> ExchangeCodeAsync(string code, string codeVerifier, Uri redirectUri, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        ArgumentNullException.ThrowIfNull(redirectUri);

        List<KeyValuePair<string, string>> formParameters =
        [
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", redirectUri.ToString()),
            new("code_verifier", codeVerifier),
        ];

        return SendTokenRequestAsync(formParameters, cancellationToken);
    }

    private async Task<XOAuth2TokenResponse> SendTokenRequestAsync(List<KeyValuePair<string, string>> formParameters, CancellationToken cancellationToken)
    {
        // AUTH-04: public clients authenticate via client_id in the body; confidential clients
        // via HTTP Basic - "You don't need client id for confidential clients with a valid
        // Authorization Header. You still are required to include Client Id in the body for the
        // requests with a public client."
        if (_clientSecret is null)
        {
            formParameters.Add(new KeyValuePair<string, string>("client_id", _clientId));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formParameters),
        };

        if (_clientSecret is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials(_clientId, _clientSecret));
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new XTransportException("OAuth 2.0 token request failed at the transport level.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await BuildTokenExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var body = await JsonSerializer.DeserializeAsync<TokenResponseBody>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);

                // AUTH-06: expiry is computed from the server's own expires_in, never a
                // hardcoded duration - if the server omits it, that's a protocol violation, not
                // something to silently paper over with a guessed default.
                if (body is null || string.IsNullOrEmpty(body.AccessToken) || body.ExpiresIn is null)
                {
                    throw new XProtocolException(
                        "OAuth 2.0 token response did not match the documented contract (missing access_token or expires_in).");
                }

                return new XOAuth2TokenResponse
                {
                    AccessToken = body.AccessToken,
                    RefreshToken = body.RefreshToken,
                    TokenType = body.TokenType ?? "bearer",
                    ExpiresAtUtc = _timeProvider.GetUtcNow() + TimeSpan.FromSeconds(body.ExpiresIn.Value),
                    Scopes = string.IsNullOrEmpty(body.Scope)
                        ? []
                        : body.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                };
            }
        }
    }

    private static async Task<XApiException> BuildTokenExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? error = null;
        string? description = null;
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var body = await JsonSerializer.DeserializeAsync<OAuth2ErrorBody>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
                error = body?.Error;
                description = body?.ErrorDescription;
            }
        }
        catch (JsonException)
        {
            // Best-effort only - fall through with the generic message below.
        }

        var message = error is null
            ? $"OAuth 2.0 token request failed (status {(int)response.StatusCode})."
            : description is null
                ? $"OAuth 2.0 token request failed: {error}."
                : $"OAuth 2.0 token request failed: {error} ({description}).";

        // AUTH-10: invalid_grant means "restart authorization", not "retry" - the exception
        // type alone signals that (XAuthenticationException); the refresh loop in a later
        // commit is what actually enforces "don't keep retrying", not this mapping.
        return new XAuthenticationException(message, response.StatusCode);
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var trimmed = query.TrimStart('?');
        if (trimmed.Length == 0)
        {
            return result;
        }

        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = pair.IndexOf('=');
            var key = separatorIndex >= 0 ? pair[..separatorIndex] : pair;
            var rawValue = separatorIndex >= 0 ? pair[(separatorIndex + 1)..] : string.Empty;
            result[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
        }

        return result;
    }

    private static string GeneratePkceCodeVerifier() => GenerateRandomUrlSafeToken(64);

    private static string ComputeS256Challenge(string codeVerifier) =>
        Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

    /// <summary>RFC 7636 verifier / CSRF state values both need a cryptographically random
    /// source (spec AUTH-01) - never <see cref="Random"/>.</summary>
    private static string GenerateRandomUrlSafeToken(int byteLength)
    {
        var bytes = new byte[byteLength];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string EncodeCredentials(string clientId, string clientSecret) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Uri.EscapeDataString(clientId)}:{Uri.EscapeDataString(clientSecret)}"));

    private sealed class TokenResponseBody
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("expires_in")]
        public long? ExpiresIn { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
    }

    private sealed class OAuth2ErrorBody
    {
        [JsonPropertyName("error")]
        public string? Error { get; init; }

        [JsonPropertyName("error_description")]
        public string? ErrorDescription { get; init; }
    }
}

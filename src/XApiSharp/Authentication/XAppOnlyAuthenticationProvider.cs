using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Authentication;

/// <summary>
/// Obtains and caches an app-only bearer token from the documented service endpoint
/// (<c>POST /oauth2/token</c> with HTTP Basic consumer-key/secret credentials and
/// <c>grant_type=client_credentials</c> - see
/// https://docs.x.com/fundamentals/authentication/oauth-2-0/application-only). This endpoint is
/// not part of the /2/ OpenAPI surface (it predates the v2 REST catalog) but is still the
/// documented, current mechanism (spec section 10.1: "the way app-only tokens are obtained must
/// be implemented per the currently-documented service endpoints").
/// <see cref="BearerTokenAuthenticationProvider"/> remains the simple path for an
/// already-obtained token; this type is for the "obtain it from consumer key/secret" scenario.
/// </summary>
public sealed class XAppOnlyAuthenticationProvider : IXAuthenticationProvider, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly string _consumerKey;
    private readonly string _consumerSecret;
    private readonly Uri _tokenEndpoint;
    private readonly Uri _revokeEndpoint;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedToken;

    public XAppOnlyAuthenticationProvider(HttpClient httpClient, string consumerKey, string consumerSecret, Uri? baseUrl = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerSecret);

        _httpClient = httpClient;
        _consumerKey = consumerKey;
        _consumerSecret = consumerSecret;
        var resolvedBaseUrl = baseUrl ?? new Uri("https://api.x.com");
        _tokenEndpoint = new Uri(resolvedBaseUrl, "oauth2/token");
        _revokeEndpoint = new Uri(resolvedBaseUrl, "oauth2/invalidate_token");
    }

    public void Dispose() => _gate.Dispose();

    public async ValueTask PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _cachedToken ?? await AcquireTokenAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>Forces a fresh token fetch even if one is cached, and revokes the previously
    /// cached token first if there was one. Not called automatically - app-only tokens don't
    /// expire under normal operation per X's docs ("valid for an application at a time... until
    /// it is invalidated"), so there is no refresh-on-401 loop here (unlike OAuth 2.0 user
    /// tokens in a later commit).</summary>
    public async Task<string> RefreshAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedToken is { } previous)
            {
                await RevokeTokenOnServerAsync(previous, cancellationToken).ConfigureAwait(false);
            }

            var token = await FetchTokenFromServerAsync(cancellationToken).ConfigureAwait(false);
            _cachedToken = token;
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Single-flight: re-check after acquiring the gate in case a concurrent caller
            // already fetched the token while this call was waiting (same shape as the
            // OAuth 2.0 user-token refresh concurrency rule, AUTH-07, applied here for app-only).
            if (_cachedToken is { } existing)
            {
                return existing;
            }

            var token = await FetchTokenFromServerAsync(cancellationToken).ConfigureAwait(false);
            _cachedToken = token;
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> FetchTokenFromServerAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials(_consumerKey, _consumerSecret));
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded") { CharSet = "UTF-8" };

        var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await BuildAuthExceptionAsync(response, "obtain", cancellationToken).ConfigureAwait(false);
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var body = await JsonSerializer.DeserializeAsync<AppOnlyTokenResponse>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
                if (body is null || !string.Equals(body.TokenType, "bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(body.AccessToken))
                {
                    throw new XProtocolException(
                        "App-only token response did not match the documented contract (expected token_type=\"bearer\" and a non-empty access_token).");
                }

                return body.AccessToken;
            }
        }
    }

    private async Task RevokeTokenOnServerAsync(string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _revokeEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", EncodeCredentials(_consumerKey, _consumerSecret));
        request.Content = new FormUrlEncodedContent([new KeyValuePair<string, string>("access_token", token)]);

        var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await BuildAuthExceptionAsync(response, "revoke", cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new XTransportException("App-only token request failed at the transport level.", ex);
        }
    }

    private static async Task<XApiException> BuildAuthExceptionAsync(HttpResponseMessage response, string action, CancellationToken cancellationToken)
    {
        string? message = null;
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                var body = await JsonSerializer.DeserializeAsync<LegacyErrorResponse>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
                message = body?.Errors?.Count > 0 ? body.Errors[0].Message : null;
            }
        }
        catch (JsonException)
        {
            // Best-effort only - fall through with the generic message below.
        }

        return new XAuthenticationException(
            message ?? $"Failed to {action} the app-only token (status {(int)response.StatusCode}).",
            response.StatusCode);
    }

    private static string EncodeCredentials(string consumerKey, string consumerSecret) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Uri.EscapeDataString(consumerKey)}:{Uri.EscapeDataString(consumerSecret)}"));

    private sealed class AppOnlyTokenResponse
    {
        [JsonPropertyName("token_type")]
        public string? TokenType { get; init; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class LegacyErrorResponse
    {
        [JsonPropertyName("errors")]
        public IReadOnlyList<LegacyError>? Errors { get; init; }
    }

    private sealed class LegacyError
    {
        [JsonPropertyName("code")]
        public int? Code { get; init; }

        [JsonPropertyName("message")]
        public string? Message { get; init; }
    }
}

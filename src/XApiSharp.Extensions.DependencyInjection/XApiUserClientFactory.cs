using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using XApiSharp.Authentication;

namespace XApiSharp.Extensions.DependencyInjection;

internal sealed class XApiUserClientFactory : IXApiUserClientFactory, IDisposable
{
    private readonly IXOAuth2TokenStoreFactory _tokenStoreFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<XClientOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, XOAuth2UserAuthenticationProvider> _providersByUser = new(StringComparer.Ordinal);

    public XApiUserClientFactory(
        string clientId,
        string? clientSecret,
        IXOAuth2TokenStoreFactory tokenStoreFactory,
        IHttpClientFactory httpClientFactory,
        IOptions<XClientOptions> options,
        TimeProvider timeProvider)
    {
        _tokenStoreFactory = tokenStoreFactory;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _timeProvider = timeProvider;

        // One shared HttpClient for the token endpoint, for this factory's lifetime (HTTP-02:
        // pooled via IHttpClientFactory, never a new handler per token request).
        OAuth2Client = new XOAuth2Client(
            httpClientFactory.CreateClient(XApiSharpDefaults.HttpClientName),
            clientId,
            clientSecret,
            options.Value.BaseUrl,
            timeProvider);
    }

    public XOAuth2Client OAuth2Client { get; }

    public XOAuth2UserAuthenticationProvider GetAuthenticationProvider(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (_providersByUser.TryGetValue(userId, out var existing))
        {
            return existing;
        }

        var created = new XOAuth2UserAuthenticationProvider(OAuth2Client, _tokenStoreFactory.GetStore(userId), _timeProvider);

        // Another caller may have raced us here - keep whichever instance actually won, and
        // dispose the loser rather than leaking its SemaphoreSlim.
        var winner = _providersByUser.GetOrAdd(userId, created);
        if (winner != created)
        {
            created.Dispose();
        }

        return winner;
    }

    public XApiClient CreateForUser(string userId)
    {
        var provider = GetAuthenticationProvider(userId);
        var httpClient = _httpClientFactory.CreateClient(XApiSharpDefaults.HttpClientName);
        return new XApiClient(httpClient, provider, _options.Value, _timeProvider);
    }

    public void Dispose()
    {
        foreach (var provider in _providersByUser.Values)
        {
            provider.Dispose();
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using XApiSharp.Authentication;

namespace XApiSharp.Extensions.DependencyInjection;

/// <summary>
/// Registration extensions for XApiSharp (spec 18.1). Two entry points, matching the two shapes
/// of auth context a caller has:
///
/// - <see cref="AddXApiSharp"/> - a single, shared <see cref="XApiClient"/> for a stateless
///   authentication mode (app-only, or any other <see cref="IXAuthenticationProvider"/> you
///   supply yourself). Safe to register - and inject - as a singleton, because nothing here
///   carries a single user's token.
/// - <see cref="AddXApiSharpMultiUser"/> - an <see cref="IXApiUserClientFactory"/> that builds a
///   fresh <see cref="XApiClient"/> per application user on demand. There is deliberately no
///   singleton <see cref="XApiClient"/> registered for this mode - a shared instance would mean
///   one user's OAuth 2.0 token leaking into another user's requests (spec 18.1: "a user token
///   must not be captured by a singleton registration of a shared client").
/// </summary>
public static class XApiSharpServiceCollectionExtensions
{
    /// <summary>
    /// Registers a single, shared <see cref="XApiClient"/> using <paramref name="authenticationProviderFactory"/> -
    /// the fully-replaceable low-level entry point (spec 18.1: "the ability to replace the token
    /// provider"). Use <see cref="AddXApiSharpAppOnly"/> for the common app-only case instead of
    /// calling this directly, unless you need a custom <see cref="IXAuthenticationProvider"/>.
    /// </summary>
    public static IServiceCollection AddXApiSharp(
        this IServiceCollection services,
        Func<IServiceProvider, IXAuthenticationProvider> authenticationProviderFactory,
        Action<XClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(authenticationProviderFactory);

        AddCoreServices(services, configureOptions);

        services.TryAddSingleton(authenticationProviderFactory);
        services.TryAddSingleton(CreateXApiClient);

        return services;
    }

    /// <summary>
    /// Registers a single, shared <see cref="XApiClient"/> authenticated with app-only OAuth 2.0
    /// (client credentials) - the common case for a read-only or automated integration with no
    /// per-user context.
    /// </summary>
    public static IServiceCollection AddXApiSharpAppOnly(
        this IServiceCollection services,
        string consumerKey,
        string consumerSecret,
        Action<XClientOptions>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerSecret);

        return services.AddXApiSharp(
            provider => new XAppOnlyAuthenticationProvider(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient(XApiSharpDefaults.HttpClientName),
                consumerKey,
                consumerSecret,
                provider.GetRequiredService<IOptions<XClientOptions>>().Value.BaseUrl),
            configureOptions);
    }

    /// <summary>
    /// Registers an <see cref="IXApiUserClientFactory"/> for building a per-user
    /// <see cref="XApiClient"/> under OAuth 2.0 Authorization Code + PKCE (spec 18.1: "a factory
    /// of clients bound to a specific authorization context"). Also registers a default,
    /// in-process <see cref="IXOAuth2TokenStoreFactory"/> (<see cref="InMemoryXOAuth2TokenStoreFactory"/>) -
    /// register your own implementation after calling this method to use durable, per-user token
    /// storage instead (see <c>docs/di-and-multi-user.md</c>).
    /// </summary>
    public static IServiceCollection AddXApiSharpMultiUser(
        this IServiceCollection services,
        string clientId,
        string? clientSecret = null,
        Action<XClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        AddCoreServices(services, configureOptions);

        services.TryAddSingleton<IXOAuth2TokenStoreFactory, InMemoryXOAuth2TokenStoreFactory>();
        services.TryAddSingleton<IXApiUserClientFactory>(provider => new XApiUserClientFactory(
            clientId,
            clientSecret,
            provider.GetRequiredService<IXOAuth2TokenStoreFactory>(),
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<IOptions<XClientOptions>>(),
            provider.GetRequiredService<TimeProvider>()));

        return services;
    }

    private static void AddCoreServices(IServiceCollection services, Action<XClientOptions>? configureOptions)
    {
        // HTTP-02: a pooled, factory-managed client - never a handler created per request.
        services.AddHttpClient(XApiSharpDefaults.HttpClientName);

        // Replaceable clock (spec 18.1): TryAdd means a TimeProvider registered by the caller,
        // either before or after this call, always wins over this default.
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<XClientOptions>();
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
    }

    private static XApiClient CreateXApiClient(IServiceProvider provider)
    {
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(XApiSharpDefaults.HttpClientName);
        var authenticationProvider = provider.GetRequiredService<IXAuthenticationProvider>();
        var options = provider.GetRequiredService<IOptions<XClientOptions>>().Value;
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        return new XApiClient(httpClient, authenticationProvider, options, timeProvider);
    }
}

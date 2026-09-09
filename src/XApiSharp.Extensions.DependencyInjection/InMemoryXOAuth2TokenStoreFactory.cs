using System.Collections.Concurrent;
using XApiSharp.Authentication;

namespace XApiSharp.Extensions.DependencyInjection;

/// <summary>
/// Default <see cref="IXOAuth2TokenStoreFactory"/>: one <see cref="XInMemoryOAuth2TokenStore"/>
/// per user ID, held for the process's lifetime. Suitable for local development and single-
/// instance apps that can tolerate losing tokens on restart; not suitable for a multi-instance
/// deployment (each instance would have its own copy) or anything that needs tokens to survive a
/// restart. Register your own <see cref="IXOAuth2TokenStoreFactory"/> after calling
/// <c>AddXApiSharpMultiUser</c> to replace this with a durable, shared implementation.
/// </summary>
public sealed class InMemoryXOAuth2TokenStoreFactory : IXOAuth2TokenStoreFactory
{
    private readonly ConcurrentDictionary<string, XInMemoryOAuth2TokenStore> _stores = new(StringComparer.Ordinal);

    public IXOAuth2TokenStore GetStore(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return _stores.GetOrAdd(userId, static _ => new XInMemoryOAuth2TokenStore());
    }
}

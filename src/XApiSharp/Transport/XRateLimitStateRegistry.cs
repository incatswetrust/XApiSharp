using System.Runtime.CompilerServices;
using XApiSharp.Authentication;

namespace XApiSharp.Transport;

/// <summary>
/// Resolves the <see cref="XRateLimitContextStore"/> for a given <see cref="IXAuthenticationProvider"/>
/// instance. A <see cref="ConditionalWeakTable{TKey,TValue}"/>, not a plain dictionary, deliberately:
/// it ties the store's lifetime to the auth provider's own lifetime (RATE-03/04's per-context state
/// is genuinely tied to that context existing) without this type ever needing to know when a
/// provider - and the user/app context it represents - goes out of use. A per-user provider that's
/// discarded (session ended, DI scope disposed) takes its rate-limit state with it for free; there
/// is no explicit cleanup call to remember and no risk of this becoming an unbounded, ever-growing
/// map of every auth context this process has ever seen.
/// </summary>
internal static class XRateLimitStateRegistry
{
    private static readonly ConditionalWeakTable<IXAuthenticationProvider, XRateLimitContextStore> Stores = new();

    public static XRateLimitContextStore GetOrCreate(IXAuthenticationProvider authenticationProvider) =>
        Stores.GetValue(authenticationProvider, static _ => new XRateLimitContextStore());
}

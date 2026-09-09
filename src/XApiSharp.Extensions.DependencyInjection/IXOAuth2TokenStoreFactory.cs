using XApiSharp.Authentication;

namespace XApiSharp.Extensions.DependencyInjection;

/// <summary>
/// Resolves the <see cref="IXOAuth2TokenStore"/> for a given application user in a multi-user
/// registration (spec 18.1: "possibility to replace the token store"). The default registration
/// uses <see cref="InMemoryXOAuth2TokenStoreFactory"/> - in-process only, lost on restart, fine
/// for local development. Replace it (register your own implementation after calling
/// <c>AddXApiSharpMultiUser</c>) with one backed by whatever durable, per-user secret storage
/// your application already uses.
/// </summary>
public interface IXOAuth2TokenStoreFactory
{
    /// <param name="userId">Your own application-level identifier for the user - whatever key
    /// your app already uses to look up that user's data.</param>
    IXOAuth2TokenStore GetStore(string userId);
}

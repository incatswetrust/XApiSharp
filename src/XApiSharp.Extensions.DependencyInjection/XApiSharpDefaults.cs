namespace XApiSharp.Extensions.DependencyInjection;

/// <summary>Shared constants for the registration extensions in this package.</summary>
public static class XApiSharpDefaults
{
    /// <summary>
    /// The name this package registers its <see cref="HttpClient"/> under via
    /// <c>IHttpClientFactory</c> (spec HTTP-02: DI must use <c>IHttpClientFactory</c> and
    /// documented lifetimes, never a new handler per request). Use this name if you need to
    /// further customize the underlying handler pipeline (a proxy, a corporate TLS root, an
    /// outgoing logging handler) via your own additional
    /// <c>services.AddHttpClient(XApiSharpDefaults.HttpClientName)</c> call - handler
    /// configuration for a named client is cumulative across multiple registrations.
    /// </summary>
    public const string HttpClientName = "XApiSharp";
}

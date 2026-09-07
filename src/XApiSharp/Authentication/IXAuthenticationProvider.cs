namespace XApiSharp.Authentication;

/// <summary>
/// Prepares authentication for a single outgoing request. Implementations must be safe to call
/// concurrently from multiple requests/contexts (API-05) and must not mutate any shared
/// <see cref="HttpClient"/> default headers (spec section 10.3).
/// </summary>
public interface IXAuthenticationProvider
{
    /// <summary>
    /// Sets whatever headers the request needs (e.g. <c>Authorization</c>) before it is sent.
    /// Implementations that need to refresh a token do so here, scoped to this call.
    /// </summary>
    ValueTask PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

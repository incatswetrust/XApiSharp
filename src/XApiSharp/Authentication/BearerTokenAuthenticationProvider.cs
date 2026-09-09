using System.Net.Http.Headers;

namespace XApiSharp.Authentication;

/// <summary>
/// Passes an already-obtained app-only bearer token on every request. Does not acquire, cache, or
/// refresh the token itself - see spec section 10.1: "passing an already-obtained token must
/// remain a simple, self-contained scenario". App-only token acquisition and OAuth 2.0 /
/// OAuth 1.0a flows land in E3.
/// </summary>
public sealed class BearerTokenAuthenticationProvider : IXAuthenticationProvider
{
    private readonly string _token;

    public BearerTokenAuthenticationProvider(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _token = token;
    }

    public ValueTask PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return ValueTask.CompletedTask;
    }
}

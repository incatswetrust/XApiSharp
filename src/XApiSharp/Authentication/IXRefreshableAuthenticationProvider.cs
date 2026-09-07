namespace XApiSharp.Authentication;

/// <summary>
/// Optional capability: an auth provider that can be told "the credential you gave me was
/// rejected, get a fresh one." <see cref="Transport.RequestExecutor"/> uses this for the
/// documented single-refresh-then-retry behavior on an expired token (spec section 13.2's retry
/// table: "Истёкший OAuth 2.0 access token | Не более одного refresh"), never more than once per
/// request regardless of how many times the retried attempt also fails.
/// </summary>
public interface IXRefreshableAuthenticationProvider : IXAuthenticationProvider
{
    Task ForceRefreshAsync(CancellationToken cancellationToken);
}

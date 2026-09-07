namespace XApiSharp;

/// <summary>
/// Client-wide settings. Minimal for the E2 vertical slice (base URL only) - timeouts, retry
/// policy, and buffer limits (spec sections 11 and 13) are added in E3 alongside the transport
/// and retry core.
/// </summary>
public sealed class XClientOptions
{
    /// <summary>
    /// The trusted API host (spec HTTP-09: fixed per official contract, overridable for tests).
    /// </summary>
    public Uri BaseUrl { get; init; } = new("https://api.x.com");
}

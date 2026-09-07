using System.Net;

namespace XApiSharp;

/// <summary>
/// A typed response body plus HTTP metadata. Not forced into a data/meta/errors shape - each
/// endpoint uses its own actual body type (spec section 12.1).
/// </summary>
/// <typeparam name="TBody">The operation's actual response body type, or a type that can
/// represent "no body" for empty/204 responses.</typeparam>
public sealed class XResponse<TBody>
{
    public required TBody? Body { get; init; }

    public required HttpStatusCode StatusCode { get; init; }

    /// <summary>Response headers, excluding secrets/authorization material.</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; init; }

    public XRateLimitInfo? RateLimit { get; init; }

    /// <summary>True when the body itself carries an <c>errors</c> array alongside <c>data</c>.</summary>
    public bool HasErrors { get; init; }

    /// <summary>True when the body has both successful elements and errors (spec section 12.1).</summary>
    public bool IsPartialSuccess { get; init; }
}

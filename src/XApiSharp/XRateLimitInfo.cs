using System.Net.Http.Headers;

namespace XApiSharp;

/// <summary>
/// Rate-limit information read from response headers, where present. All fields are nullable:
/// a missing or unparsable header is never treated as zero or unlimited (RATE-02).
/// </summary>
public sealed class XRateLimitInfo
{
    /// <summary>Value of <c>x-rate-limit-limit</c>, if present and parsable.</summary>
    public int? Limit { get; init; }

    /// <summary>Value of <c>x-rate-limit-remaining</c>, if present and parsable.</summary>
    public int? Remaining { get; init; }

    /// <summary>Value of <c>x-rate-limit-reset</c> (Unix seconds), if present and parsable.</summary>
    public DateTimeOffset? Reset { get; init; }

    internal static XRateLimitInfo? FromHeaders(HttpResponseHeaders headers)
    {
        var limit = ParseInt(headers, "x-rate-limit-limit");
        var remaining = ParseInt(headers, "x-rate-limit-remaining");
        var reset = ParseUnixSeconds(headers, "x-rate-limit-reset");

        if (limit is null && remaining is null && reset is null)
        {
            return null;
        }

        return new XRateLimitInfo { Limit = limit, Remaining = remaining, Reset = reset };
    }

    private static int? ParseInt(HttpResponseHeaders headers, string name)
    {
        if (headers.TryGetValues(name, out var values)
            && int.TryParse(values.FirstOrDefault(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTimeOffset? ParseUnixSeconds(HttpResponseHeaders headers, string name)
    {
        if (headers.TryGetValues(name, out var values)
            && long.TryParse(values.FirstOrDefault(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }

        return null;
    }
}

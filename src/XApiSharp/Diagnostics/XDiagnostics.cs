using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace XApiSharp.Diagnostics;

/// <summary>
/// The SDK's <see cref="ActivitySource"/>/<see cref="Meter"/> (spec section 18.2: "use
/// ActivitySource and Meter from the BCL; the consumer can plug in OpenTelemetry from their own
/// app"). This type never talks to any exporter itself - listening (via
/// <c>ActivityListener</c>/<c>MeterListener</c>, or an OpenTelemetry SDK) is entirely the
/// consumer's choice; with nothing listening, every call below is close to free (a null-check on
/// <c>System.Diagnostics.Activity.Current</c> and a no-op instrument record).
///
/// Tag values are restricted by design to route templates and small enums/counts - never a user
/// ID, a real URL with its query string, a token, or Post/message text (spec 18.2's explicit
/// list). <see cref="ToRouteTemplate"/> is a best-effort redaction of the one case where the SDK
/// doesn't already have a clean template on hand (see its own remarks) - stream operations pass
/// their fixed, literal path directly instead, since those never interpolate caller data.
/// </summary>
internal static class XDiagnostics
{
    public const string Name = "XApiSharp";

    private static readonly string Version = typeof(XDiagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    public static readonly ActivitySource ActivitySource = new(Name, Version);

    private static readonly Meter Meter = new(Name, Version);

    public static readonly Counter<long> RequestCount = Meter.CreateCounter<long>(
        "xapisharp.requests", unit: "{request}", description: "Number of API requests attempted.");

    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "xapisharp.request.duration", unit: "ms", description: "Duration of one logical API request, including retries.");

    public static readonly Counter<long> RetryCount = Meter.CreateCounter<long>(
        "xapisharp.retries", unit: "{retry}", description: "Number of retry attempts.");

    public static readonly Counter<long> ErrorCount = Meter.CreateCounter<long>(
        "xapisharp.errors", unit: "{error}", description: "Number of requests that ended in an exception.");

    public static readonly UpDownCounter<long> ActiveStreamConnections = Meter.CreateUpDownCounter<long>(
        "xapisharp.stream.active_connections", unit: "{connection}", description: "Number of currently-open streaming connections.");

    public static readonly Counter<long> DroppedStreamEvents = Meter.CreateCounter<long>(
        "xapisharp.stream.dropped_events", unit: "{event}", description: "Number of streaming events dropped as duplicates by XStreamOptions.DeduplicationKey.");

    /// <summary>
    /// Best-effort route template for a request path that may contain interpolated identifiers
    /// (e.g. <c>2/users/1234567890</c> -&gt; <c>2/users/{id}</c>). This SDK builds request paths
    /// by string interpolation at ~190 call sites rather than from a declared route table, so
    /// there is no ground-truth template to read - this redacts any path segment that looks like
    /// a dynamic value (purely numeric, or containing a percent-escape from
    /// <see cref="Uri.EscapeDataString(string)"/>) rather than a literal path keyword. It will not
    /// catch every case (a plain-alphanumeric username in a path segment is indistinguishable
    /// from a fixed keyword by this heuristic) - it exists to keep the common case (numeric
    /// Snowflake IDs, which are the overwhelming majority of this SDK's path parameters) out of
    /// metric tags, not to give an absolute guarantee. The leading segment (the fixed <c>2</c>
    /// API version prefix on every path) is always exempt from redaction.
    /// </summary>
    public static string ToRouteTemplate(string relativePath)
    {
        var segments = relativePath.Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            // Segment 0 is always the fixed API version prefix ("2") - never a dynamic value -
            // so it's exempt even though it would otherwise look purely numeric.
            if (i == 0 || segments[i].Length == 0)
            {
                continue;
            }

            var segment = segments[i];

            var looksDynamic = segment.Contains('%', StringComparison.Ordinal);
            if (!looksDynamic)
            {
                looksDynamic = true;
                foreach (var c in segment)
                {
                    if (!char.IsAsciiDigit(c))
                    {
                        looksDynamic = false;
                        break;
                    }
                }
            }

            if (looksDynamic)
            {
                segments[i] = "{id}";
            }
        }

        return string.Join('/', segments);
    }

    /// <summary>Adds a named event to the currently-active <see cref="Activity"/>, if any (spec
    /// 18.2's minimal events: retry, rate-limit wait, refresh, stream connect/disconnect,
    /// deserialization error). A no-op when nothing is listening/tracing.</summary>
    public static void AddEvent(string name, params ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        // Fully qualified: this file's namespace shares a parent (XApiSharp) with the unrelated
        // XApiSharp.Activity family namespace, which would otherwise shadow System.Diagnostics.Activity.
        var activity = System.Diagnostics.Activity.Current;
        if (activity is null || !activity.IsAllDataRequested)
        {
            return;
        }

        activity.AddEvent(new ActivityEvent(name, tags: new ActivityTagsCollection(tags.ToArray())));
    }
}

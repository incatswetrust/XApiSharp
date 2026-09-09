using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Diagnostics;
using XApiSharp.Streaming;
using XApiSharp.Users;
using ActivityAlias = System.Diagnostics.Activity;

namespace XApiSharp.UnitTests;

/// <summary>
/// Spec section 18.2: minimal diagnostics (ActivitySource/Meter, route-template-only tags). These
/// tests listen the same way an OpenTelemetry SDK would (ActivityListener/MeterListener) rather
/// than reaching into XDiagnostics' internals, so they also double as proof the instrumentation
/// is actually observable from outside the assembly the way a consumer would see it.
/// <see cref="ActivityAlias"/> avoids <see cref="System.Diagnostics.Activity"/> being shadowed by
/// the unrelated <c>XApiSharp.Activity</c> family namespace.
///
/// XDiagnostics' ActivitySource/Meter are process-wide singletons, and xUnit runs test classes in
/// this assembly in parallel with each other by default - so a listener registered here can also
/// observe activity from a *different*, concurrently-running test class hitting the very same
/// route (route tags deliberately carry no per-call identifier - that's the point of route
/// templating). Assertions below use "at least one/contains" rather than "exactly one", so this
/// stays robust under that parallelism instead of being flaky, while still proving the
/// instrumentation actually fires and carries the right shape.
/// </summary>
public class XDiagnosticsTests
{
    [Theory]
    [InlineData("2/users/1234567890", "2/users/{id}")]
    [InlineData("2/users/by/username/jack", "2/users/by/username/jack")]
    [InlineData("2/media/upload/abc%20def/finalize", "2/media/upload/{id}/finalize")]
    [InlineData("2/tweets/search/stream", "2/tweets/search/stream")]
    public void ToRouteTemplate_redacts_numeric_and_percent_encoded_segments_only(string path, string expected) =>
        Assert.Equal(expected, XDiagnostics.ToRouteTemplate(path));

    [Fact]
    public async Task A_successful_request_records_an_activity_and_request_count()
    {
        const string route = "2/users/{id}";
        const string userId = "990011223344556677";

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == XDiagnostics.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        var activities = new List<ActivityAlias>();
        activityListener.ActivityStopped = activities.Add;
        ActivitySource.AddActivityListener(activityListener);

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == XDiagnostics.Name)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        var requestCount = 0;
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "xapisharp.requests" && HasRouteTag(tags, route))
            {
                requestCount += (int)measurement;
            }
        });
        meterListener.Start();

        using var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"data\":{\"id\":\"" + userId + "\",\"name\":\"A\",\"username\":\"a\"}}",
                Encoding.UTF8,
                "application/json"),
        }));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        await client.Users.GetByIdAsync(new GetUserRequest { Id = userId });

        Assert.Contains(activities, a =>
            (string?)a.GetTagItem("url.template") == route
            && a.Status == ActivityStatusCode.Ok
            && !a.DisplayName.Contains(userId, StringComparison.Ordinal));

        Assert.True(requestCount >= 1, "Expected at least one xapisharp.requests measurement for this route.");
    }

    [Fact]
    public async Task A_stream_connection_increments_and_then_decrements_the_active_connections_gauge()
    {
        const string route = "2/tweets/sample/stream";

        using var meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == XDiagnostics.Name)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };
        var deltas = new List<long>();
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name == "xapisharp.stream.active_connections" && HasRouteTag(tags, route))
            {
                deltas.Add(measurement);
            }
        });
        meterListener.Start();

        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"data\":{\"id\":\"1\"}}\n"))),
            };
            return Task.FromResult(response);
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        await foreach (var _ in client.Streaming.StreamPostsSampleAsync(new PostSampleStreamRequest()))
        {
            // Break after the fake response's one line, before EOF would otherwise surface as
            // XStreamConnectionLostException - STREAM-12: stopping enumeration is a caller
            // decision, and disposing the enumerator here is what should run the disconnect path.
            break;
        }

        Assert.Contains(1L, deltas);
        Assert.Contains(-1L, deltas);
    }

    private static bool HasRouteTag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string route)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == "url.template" && (string?)tag.Value == route)
            {
                return true;
            }
        }

        return false;
    }
}

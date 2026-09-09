using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Errors;

namespace XApiSharp.UnitTests;

/// <summary>MEDIA-08-style bounded-deadline coverage for the Compliance Jobs polling helper
/// (spec section 17.2) - driven by FakeTimeProvider (no real multi-second waits), same pattern as
/// RetryTests/MediaUploadTimingTests.</summary>
public class ComplianceJobWaitTests
{
    [Fact]
    public async Task WaitForCompletionAsync_polls_until_the_job_completes()
    {
        var callCount = 0;
        var timeProvider = new FakeTimeProvider();
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/compliance/jobs/1", request.RequestUri!.AbsolutePath);
            callCount++;
            var status = callCount < 3 ? "in_progress" : "complete";
            return Task.FromResult(SuccessResponse($"{{\"data\":{{\"id\":\"1\",\"status\":\"{status}\"}}}}"));
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider);

        await using var pump = StartPump(timeProvider);

        var job = await client.Compliance.WaitForCompletionAsync("1", new XApiSharp.Compliance.XJobWaitOptions
        {
            PollInterval = TimeSpan.FromSeconds(1),
        });

        Assert.Equal("complete", job.Status);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task WaitForCompletionAsync_throws_with_the_last_known_status_when_the_job_fails()
    {
        using var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(SuccessResponse("""{"data":{"id":"1","status":"failed"}}""")));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        var ex = await Assert.ThrowsAsync<XJobPollingException>(() => client.Compliance.WaitForCompletionAsync("1"));

        Assert.Equal("1", ex.JobId);
        Assert.Equal("failed", ex.LastKnownStatus);
    }

    [Fact]
    public async Task WaitForCompletionAsync_throws_when_the_deadline_is_exceeded()
    {
        var timeProvider = new FakeTimeProvider();
        using var handler = new FakeHttpMessageHandler((_, _) =>
            Task.FromResult(SuccessResponse("""{"data":{"id":"1","status":"in_progress"}}""")));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider);

        await using var pump = StartPump(timeProvider);

        var ex = await Assert.ThrowsAsync<XJobPollingException>(() => client.Compliance.WaitForCompletionAsync("1", new XApiSharp.Compliance.XJobWaitOptions
        {
            PollInterval = TimeSpan.FromSeconds(1),
            Timeout = TimeSpan.FromSeconds(2),
        }));

        Assert.Equal("in_progress", ex.LastKnownStatus);
        Assert.Contains("Timed out", ex.Message, StringComparison.Ordinal);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static Pump StartPump(FakeTimeProvider timeProvider)
    {
        var cts = new CancellationTokenSource();
        var pumpTask = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                timeProvider.Advance(TimeSpan.FromSeconds(1));
                await Task.Delay(1, CancellationToken.None).ConfigureAwait(false);
            }
        });

        return new Pump(cts, pumpTask);
    }

    private sealed class Pump(CancellationTokenSource cts, Task pumpTask) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await cts.CancelAsync();
            await pumpTask;
            cts.Dispose();
        }
    }
}

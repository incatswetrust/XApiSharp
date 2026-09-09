using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Media;

namespace XApiSharp.UnitTests;

/// <summary>MEDIA-08 - the post-finalize processing-status poll loop's bounded deadline, driven
/// by FakeTimeProvider (no real multi-second waits), same pattern as RetryTests/TimeoutTests.</summary>
public class MediaUploadTimingTests
{
    [Fact]
    public async Task UploadFromStreamAsync_polls_until_processing_succeeds()
    {
        var statusCallCount = 0;
        var timeProvider = new FakeTimeProvider();
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/2/media/upload/initialize")
            {
                return Task.FromResult(SuccessResponse("""{"data":{"id":"m1"}}"""));
            }

            if (path == "/2/media/upload/m1/finalize")
            {
                return Task.FromResult(SuccessResponse("""{"data":{"id":"m1","processing_info":{"state":"pending","check_after_secs":1}}}"""));
            }

            if (path == "/2/media/upload" && request.RequestUri.Query.Contains("command=STATUS", StringComparison.Ordinal))
            {
                statusCallCount++;
                var state = statusCallCount < 2 ? "in_progress" : "succeeded";
                return Task.FromResult(SuccessResponse($"{{\"data\":{{\"id\":\"m1\",\"processing_info\":{{\"state\":\"{state}\",\"check_after_secs\":1}}}}}}"));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {path}");
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider);
        using var stream = new MemoryStream([]);

        await using var pump = StartPump(timeProvider);

        var info = await client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            TotalBytes = 0,
        });

        Assert.Equal("succeeded", info.ProcessingInfo!.State);
        Assert.Equal(2, statusCallCount);
    }

    [Fact]
    public async Task UploadFromStreamAsync_throws_when_the_processing_deadline_is_exceeded()
    {
        var timeProvider = new FakeTimeProvider();
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/2/media/upload/initialize")
            {
                return Task.FromResult(SuccessResponse("""{"data":{"id":"m1"}}"""));
            }

            if (path == "/2/media/upload/m1/finalize")
            {
                // check_after_secs (100s) alone exceeds the 2s ProcessingTimeout below, so the
                // deadline check fires before any poll wait/request is needed.
                return Task.FromResult(SuccessResponse("""{"data":{"id":"m1","processing_info":{"state":"pending","check_after_secs":100}}}"""));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {path}");
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), timeProvider: timeProvider);
        using var stream = new MemoryStream([]);

        var ex = await Assert.ThrowsAsync<XMediaUploadException>(() => client.Media.UploadFromStreamAsync(new UploadFromStreamRequest
        {
            Media = stream,
            MediaCategory = XMediaCategory.TweetVideo,
            MediaType = XMediaMimeType.VideoMp4,
            TotalBytes = 0,
            ProcessingTimeout = TimeSpan.FromSeconds(2),
        }));

        Assert.Equal("pending", ex.LastKnownState);
        Assert.Contains("Timed out", ex.Message, StringComparison.Ordinal);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    /// <summary>Repeatedly advances the fake clock so any pending FakeTimeProvider-backed delay
    /// completes, without a real multi-second wait - same pattern as RetryTests.</summary>
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

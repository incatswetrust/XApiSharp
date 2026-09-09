using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using XApiSharp.Errors;
using XApiSharp.Streaming;

namespace XApiSharp.UnitTests;

/// <summary>
/// Unit coverage for the shared streaming engine (spec section 16, STREAM-01..12). No real HTTP
/// is involved - the connect delegate hands back an in-memory response directly, since
/// <see cref="XEventStream"/> only cares about the connect/options contract, not how a connection
/// is actually opened (same approach as <see cref="XPaginatorTests"/> for the pagination engine).
/// </summary>
public class XEventStreamTests
{
    private sealed record FakeEvent(string Id);

    [Fact]
    public void Does_not_connect_before_enumeration_begins()
    {
        var connectCount = 0;
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => { connectCount++; return Task.FromResult(ResponseFor("")); },
            options: null,
            TimeProvider.System,
            cancellationToken: default);

        Assert.Equal(0, connectCount);
    }

    [Fact]
    public async Task Skips_blank_heartbeat_lines_and_yields_the_events()
    {
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => Task.FromResult(ResponseFor("\r\n{\"id\":\"1\"}\n\n{\"id\":\"2\"}\n")),
            options: null,
            TimeProvider.System,
            cancellationToken: default);

        var events = new List<FakeEvent>();
        await foreach (var e in enumerable)
        {
            events.Add(e);
            if (events.Count == 2)
            {
                break;
            }
        }

        Assert.Equal(["1", "2"], events.Select(e => e.Id));
    }

    [Fact]
    public async Task Throws_malformed_message_exception_for_invalid_json_and_does_not_reconnect()
    {
        var connectCount = 0;
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => { connectCount++; return Task.FromResult(ResponseFor("not-json\n")); },
            new XStreamOptions<FakeEvent> { Reconnect = new XStreamReconnectOptions { MaxAttempts = 5 } },
            TimeProvider.System,
            cancellationToken: default);

        await Assert.ThrowsAsync<XStreamMalformedMessageException>(async () =>
        {
            await foreach (var _ in enumerable)
            {
            }
        });

        Assert.Equal(1, connectCount);
    }

    [Fact]
    public async Task Deduplicates_events_sharing_the_same_key()
    {
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => Task.FromResult(ResponseFor("{\"id\":\"1\"}\n{\"id\":\"1\"}\n{\"id\":\"2\"}\n")),
            new XStreamOptions<FakeEvent> { DeduplicationKey = e => e.Id },
            TimeProvider.System,
            cancellationToken: default);

        var events = new List<FakeEvent>();
        await foreach (var e in enumerable)
        {
            events.Add(e);
            if (e.Id == "2")
            {
                break;
            }
        }

        Assert.Equal(["1", "2"], events.Select(e => e.Id));
    }

    [Fact]
    public async Task Throws_connection_lost_on_a_clean_disconnect_when_reconnect_is_not_configured()
    {
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => Task.FromResult(ResponseFor("{\"id\":\"1\"}\n")),
            options: null,
            TimeProvider.System,
            cancellationToken: default);

        var events = new List<FakeEvent>();
        await Assert.ThrowsAsync<XStreamConnectionLostException>(async () =>
        {
            await foreach (var e in enumerable)
            {
                events.Add(e);
            }
        });

        Assert.Equal(["1"], events.Select(e => e.Id));
    }

    [Fact]
    public async Task Reconnects_on_a_clean_disconnect_and_keeps_yielding_from_the_new_connection()
    {
        var connectCount = 0;
        var reconnectReports = new List<XStreamReconnectEvent>();
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ =>
            {
                connectCount++;
                var body = connectCount == 1 ? "{\"id\":\"1\"}\n" : "{\"id\":\"2\"}\n";
                return Task.FromResult(ResponseFor(body));
            },
            new XStreamOptions<FakeEvent>
            {
                Reconnect = new XStreamReconnectOptions
                {
                    MaxAttempts = 3,
                    InitialBackoff = TimeSpan.FromMilliseconds(1),
                    OnReconnecting = new Progress<XStreamReconnectEvent>(reconnectReports.Add),
                },
            },
            TimeProvider.System,
            cancellationToken: default);

        var events = new List<FakeEvent>();
        await foreach (var e in enumerable)
        {
            events.Add(e);
            if (events.Count == 2)
            {
                break;
            }
        }

        Assert.Equal(["1", "2"], events.Select(e => e.Id));
        Assert.Equal(2, connectCount);
        Assert.Single(reconnectReports);
        Assert.IsType<XStreamConnectionLostException>(reconnectReports[0].Cause);
    }

    [Fact]
    public async Task Gives_up_once_MaxAttempts_reconnects_are_exhausted()
    {
        var connectCount = 0;
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => { connectCount++; return Task.FromResult(ResponseFor("")); }, // immediate clean EOF every time
            new XStreamOptions<FakeEvent>
            {
                Reconnect = new XStreamReconnectOptions { MaxAttempts = 2, InitialBackoff = TimeSpan.FromMilliseconds(1) },
            },
            TimeProvider.System,
            cancellationToken: default);

        await Assert.ThrowsAsync<XStreamConnectionLostException>(async () =>
        {
            await foreach (var _ in enumerable)
            {
            }
        });

        Assert.Equal(3, connectCount); // initial connection + 2 reconnects, then give up
    }

    [Fact]
    public async Task Throws_idle_timeout_when_no_data_arrives_in_time()
    {
        var timeProvider = new FakeTimeProvider();
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => Task.FromResult(ResponseFor(new NeverEndingStream())),
            new XStreamOptions<FakeEvent> { IdleTimeout = TimeSpan.FromSeconds(30) },
            timeProvider,
            cancellationToken: default);

        await using var pump = StartPump(timeProvider);

        await Assert.ThrowsAsync<XStreamIdleTimeoutException>(async () =>
        {
            await foreach (var _ in enumerable)
            {
            }
        });
    }

    [Fact]
    public async Task Closes_the_connection_when_the_caller_stops_enumerating_early()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("{\"id\":\"1\"}\n{\"id\":\"2\"}\n{\"id\":\"3\"}\n"));
        var enumerable = XEventStream.EnumerateAsync<FakeEvent>(
            _ => Task.FromResult(ResponseFor(stream)),
            options: null,
            TimeProvider.System,
            cancellationToken: default);

        await foreach (var e in enumerable)
        {
            Assert.Equal("1", e.Id);
            break;
        }

        Assert.False(stream.CanRead); // STREAM-12: disposed via the response's cascading Dispose
    }

    private static HttpResponseMessage ResponseFor(string body) =>
        ResponseFor(new MemoryStream(Encoding.UTF8.GetBytes(body)));

    private static HttpResponseMessage ResponseFor(Stream body) =>
        new(HttpStatusCode.OK) { Content = new StreamContent(body) };

    /// <summary>A stream whose <c>ReadAsync</c> never completes on its own - only the caller's
    /// cancellation token can end the read, simulating a connection with no heartbeat.</summary>
    private sealed class NeverEndingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

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

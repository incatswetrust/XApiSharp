using System.Runtime.CompilerServices;
using System.Text.Json;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>
/// The one shared engine every streaming family method builds on (spec section 16) - STREAM-01
/// through STREAM-12 are implemented once here, mirroring how <see cref="Pagination.XPaginator"/>
/// is the one shared pagination engine. A family method supplies a connect delegate (open a fresh
/// HTTP connection for the fixed method/path/query, called again on every reconnect attempt) and
/// gets back a pull-based <see cref="IAsyncEnumerable{T}"/> of already-deserialized events.
/// </summary>
internal static class XEventStream
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// STREAM-01/02/03 are handled by <see cref="XStreamLineReader"/>. STREAM-04: this is an
    /// iterator method with no internal queue - a line is only read once the consumer calls
    /// <c>MoveNextAsync</c>, so a slow consumer simply leaves bytes unread in the OS/HttpClient
    /// buffer rather than accumulating anywhere in this engine. STREAM-05: the only event loss
    /// vector is a reconnect gap, reported via <see cref="XStreamReconnectOptions.OnReconnecting"/>
    /// rather than happening silently. STREAM-06: connect-time auth/rate-limit failures keep their
    /// existing typed exceptions (<see cref="XAuthenticationException"/>/<see cref="XAccessDeniedException"/>/
    /// <see cref="XRateLimitException"/>) from <c>RequestExecutor.OpenStreamAsync</c>; a lost
    /// mid-stream connection is <see cref="XStreamConnectionLostException"/>. STREAM-07: reconnect
    /// only happens when <paramref name="options"/>.Reconnect is non-null, with backoff, and gives
    /// up permanently past <c>MaxAttempts</c> or on a non-retryable exception (STREAM-11).
    /// STREAM-08 is the per-line read deadline below. STREAM-09: <c>backfill_minutes</c> is a
    /// caller-supplied request parameter, not something this engine fabricates. STREAM-10 is the
    /// optional dedup window. STREAM-12: every <c>await using</c>/<c>using</c> here is scoped to
    /// one connection attempt, so breaking out of the caller's <c>await foreach</c> (or the
    /// iterator being disposed on cancellation) closes the in-flight response/stream through the
    /// compiler-generated <c>finally</c>, the same guarantee <see cref="Pagination.XPaginator"/>
    /// relies on for PAGE-07. No event payload content is ever included in an exception message
    /// here - only counts, states, and byte-size numbers.
    /// </summary>
    /// <param name="connect">Opens a fresh, already-authenticated HTTP connection for this
    /// stream's fixed method/path/query. Invoked once per connection attempt (including
    /// reconnects) - never call it more than once per attempt or the caller sees duplicate
    /// connects.</param>
    /// <param name="options">Tuning knobs; <see langword="null"/> behaves like
    /// <c>new XStreamOptions&lt;TBody&gt;()</c>.</param>
    /// <param name="timeProvider">Drives idle-timeout and reconnect-backoff waits, so tests never
    /// need a real sleep.</param>
    /// <param name="cancellationToken">Observed before each connect and each line read; disposing
    /// the enumerator (including via an early <c>break</c>) closes the in-flight connection.</param>
    public static async IAsyncEnumerable<TBody> EnumerateAsync<TBody>(
        Func<CancellationToken, Task<HttpResponseMessage>> connect,
        XStreamOptions<TBody>? options,
        TimeProvider timeProvider,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connect);
        options ??= new XStreamOptions<TBody>();

        var dedupSeen = options.DeduplicationKey is null ? null : new HashSet<string>(StringComparer.Ordinal);
        var dedupOrder = options.DeduplicationKey is null ? null : new Queue<string>();

        var attempt = 0;
        while (true)
        {
            attempt++;
            cancellationToken.ThrowIfCancellationRequested();

            HttpResponseMessage response;
            try
            {
                response = await connect(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldReconnect(ex, options.Reconnect, attempt))
            {
                await ReportAndWaitAsync(options.Reconnect!, attempt, ex, timeProvider, cancellationToken).ConfigureAwait(false);
                continue;
            }

            Exception? connectionLostCause = null;
            using (response)
            {
                var rawStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var reader = new XStreamLineReader(rawStream, options.MaxMessageSizeBytes);
                await using (reader.ConfigureAwait(false))
                {
                    while (true)
                    {
                        string? line;
                        CancellationTokenSource? idleTimeoutCts = null;
                        CancellationTokenSource? linkedCts = null;
                        var timedOutOrLost = false;
                        try
                        {
                            var readToken = cancellationToken;
                            if (options.IdleTimeout is { } idleTimeout)
                            {
                                idleTimeoutCts = new CancellationTokenSource(idleTimeout, timeProvider);
                                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, idleTimeoutCts.Token);
                                readToken = linkedCts.Token;
                            }

                            line = await reader.ReadLineAsync(readToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (idleTimeoutCts is { IsCancellationRequested: true } && !cancellationToken.IsCancellationRequested)
                        {
                            // STREAM-06/08: distinguishable from a caller-driven cancellation (that
                            // one is never caught here - it propagates as OperationCanceledException)
                            // and, like any other connection-loss shape, eligible for reconnect
                            // below rather than always ending the enumeration outright.
                            connectionLostCause = new XStreamIdleTimeoutException(
                                $"No data was received from the stream within the configured idle timeout of {options.IdleTimeout}.");
                            timedOutOrLost = true;
                            line = null;
                        }
                        catch (IOException ex)
                        {
                            connectionLostCause = new XStreamConnectionLostException("The stream connection was lost while reading.", ex);
                            timedOutOrLost = true;
                            line = null;
                        }
                        catch (HttpRequestException ex)
                        {
                            connectionLostCause = new XStreamConnectionLostException("The stream connection was lost while reading.", ex);
                            timedOutOrLost = true;
                            line = null;
                        }
                        finally
                        {
                            linkedCts?.Dispose();
                            idleTimeoutCts?.Dispose();
                        }

                        if (timedOutOrLost)
                        {
                            break;
                        }

                        if (line is null)
                        {
                            connectionLostCause = new XStreamConnectionLostException("The server closed the stream connection.");
                            break;
                        }

                        if (line.Length == 0 || IsWhitespaceOnly(line))
                        {
                            // STREAM-01: a blank line is a heartbeat/keep-alive, not an event -
                            // still counts as activity for the idle timeout above, just no body
                            // to deserialize or yield.
                            continue;
                        }

                        TBody body;
                        try
                        {
                            body = JsonSerializer.Deserialize<TBody>(line, JsonOptions)!;
                        }
                        catch (JsonException ex)
                        {
                            throw new XStreamMalformedMessageException("A stream message was not valid JSON for the expected contract.", ex);
                        }

                        if (dedupSeen is not null)
                        {
                            var key = options.DeduplicationKey!(body);
                            if (!dedupSeen.Add(key))
                            {
                                continue;
                            }

                            dedupOrder!.Enqueue(key);
                            if (dedupOrder.Count > options.DeduplicationWindowSize)
                            {
                                dedupSeen.Remove(dedupOrder.Dequeue());
                            }
                        }

                        yield return body;
                    }
                }
            }

            if (!ShouldReconnect(connectionLostCause!, options.Reconnect, attempt))
            {
                throw connectionLostCause!;
            }

            await ReportAndWaitAsync(options.Reconnect!, attempt, connectionLostCause!, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsWhitespaceOnly(string line)
    {
        foreach (var c in line)
        {
            if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>STREAM-06/11: only network-loss-shaped and rate-limit-shaped failures are ever
    /// eligible for reconnect. Authentication/access errors, oversized messages, and malformed
    /// JSON are permanent from this engine's point of view and always propagate.</summary>
    private static bool ShouldReconnect(Exception ex, XStreamReconnectOptions? policy, int attemptNumber)
    {
        if (policy is null || attemptNumber > policy.MaxAttempts)
        {
            return false;
        }

        return ex is XStreamConnectionLostException or XStreamIdleTimeoutException or XTransportException or XRateLimitException;
    }

    private static async Task ReportAndWaitAsync(XStreamReconnectOptions policy, int attemptNumber, Exception cause, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        // STREAM-05: report the reconnect (and therefore the potential event gap) before
        // waiting - a throwing callback propagates, it is not swallowed.
        policy.OnReconnecting?.Report(new XStreamReconnectEvent(attemptNumber, cause));

        var exponential = TimeSpan.FromMilliseconds(policy.InitialBackoff.TotalMilliseconds * Math.Pow(2, attemptNumber - 1));
        var capped = exponential > policy.MaxBackoff ? policy.MaxBackoff : exponential;
        var jittered = capped * Random.Shared.NextDouble();

        await Task.Delay(jittered, timeProvider, cancellationToken).ConfigureAwait(false);
    }
}

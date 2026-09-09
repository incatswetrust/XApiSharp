using Microsoft.Extensions.Time.Testing;
using XApiSharp.Authentication;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>HTTP-07/HTTP-08 - all deadline-related behavior driven by TimeProvider, no real
/// sleeps (spec: "every timeout-dependent test goes through TimeProvider").</summary>
public class TimeoutTests
{
    [Fact]
    public async Task Operation_timeout_throws_XRequestTimeoutException_without_a_real_wait()
    {
        var timeProvider = new FakeTimeProvider();
        var options = new XClientOptions { OperationTimeout = TimeSpan.FromSeconds(5), AttemptTimeout = TimeSpan.FromSeconds(5) };

        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            // Simulate a hang: advance the fake clock past the deadline instead of sleeping.
            timeProvider.Advance(TimeSpan.FromSeconds(6));
            await Task.Delay(Timeout.Infinite, ct); // observes the real cancellation triggered by the advance
            throw new UnreachableException();
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options, timeProvider);

        var ex = await Assert.ThrowsAsync<XRequestTimeoutException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Contains("OperationTimeout", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Attempt_timeout_fires_separately_from_operation_timeout()
    {
        var timeProvider = new FakeTimeProvider();
        // AttemptTimeout shorter than OperationTimeout: the attempt should time out first.
        var options = new XClientOptions { OperationTimeout = TimeSpan.FromSeconds(60), AttemptTimeout = TimeSpan.FromSeconds(5) };

        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            timeProvider.Advance(TimeSpan.FromSeconds(6));
            await Task.Delay(Timeout.Infinite, ct);
            throw new UnreachableException();
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"), options, timeProvider);

        var ex = await Assert.ThrowsAsync<XRequestTimeoutException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Contains("AttemptTimeout", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task External_HttpClient_Timeout_is_reported_as_XRequestTimeoutException()
    {
        // HTTP-08: simulate exactly the exception shape the BCL throws when HttpClient.Timeout
        // (not our own tokens) fires, without needing a real HttpClient.Timeout to elapse.
        using var handler = new FakeHttpMessageHandler((_, _) =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException()));
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        var ex = await Assert.ThrowsAsync<XRequestTimeoutException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }));

        Assert.Contains("HttpClient.Timeout", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Caller_cancellation_is_reported_as_is_even_with_timeouts_configured()
    {
        using var cts = new CancellationTokenSource();
        using var handler = new FakeHttpMessageHandler(async (_, ct) =>
        {
            await cts.CancelAsync();
            await Task.Delay(Timeout.Infinite, ct);
            throw new UnreachableException();
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("t"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "1" }, cts.Token));
    }
}

file sealed class UnreachableException : Exception;

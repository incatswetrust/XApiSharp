using XApiSharp.Authentication;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

public class CancellationTests
{
    [Fact]
    public async Task GetByIdAsync_honors_an_already_cancelled_token()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var handlerCalled = false;
        using var handler = new FakeHttpMessageHandler((_, _) =>
        {
            handlerCalled = true;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });
        using var httpClient = new HttpClient(handler);
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("token"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.Users.GetByIdAsync(new GetUserRequest { Id = "123" }, cts.Token));

        Assert.False(handlerCalled, "the request should not reach the transport once already cancelled");
    }
}

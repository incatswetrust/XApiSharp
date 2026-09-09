using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using XApiSharp;
using XApiSharp.Extensions.DependencyInjection;
using XApiSharp.Users;

namespace XApiSharp.PackageTests;

/// <summary>
/// Spec section 22.3: "separately verify DI registration" - against the packed
/// <c>XApiSharp.Net.Extensions.DependencyInjection</c> package, not source.
/// </summary>
public class DiRegistrationTests
{
    [Fact]
    public async Task AddXApiSharpAppOnly_resolves_a_working_client()
    {
        var services = new ServiceCollection();
        services.AddXApiSharpAppOnly("consumer-key", "consumer-secret");

        // Swap in a fake handler for the shared named client (docs/di-and-multi-user.md's own
        // documented customization point) so this test never makes a real network call.
        services.AddHttpClient(XApiSharpDefaults.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHttpMessageHandler((request, _) =>
            {
                var body = request.RequestUri!.AbsolutePath.Contains("oauth2/token", StringComparison.Ordinal)
                    ? """{"token_type":"bearer","access_token":"fake-app-only-token","expires_in":7200}"""
                    : """{"data":{"id":"1","username":"a"}}""";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                });
            }));

        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<XApiClient>();
        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        Assert.Equal("a", response.Body?.Data?.Username);
    }

    [Fact]
    public void AddXApiSharpMultiUser_resolves_a_per_user_client_factory()
    {
        var services = new ServiceCollection();
        services.AddXApiSharpMultiUser("client-id", "client-secret");

        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IXApiUserClientFactory>();
        Assert.NotNull(factory.OAuth2Client);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}

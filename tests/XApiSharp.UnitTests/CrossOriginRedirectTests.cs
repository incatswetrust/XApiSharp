using System.Net;
using System.Net.Sockets;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>
/// HTTP-10: real transport test (spec section 19.1 "Transport" tier - actual local HTTP servers,
/// not a fake handler) proving a cross-origin redirect does not carry the Authorization header
/// to the new host. This is BCL (SocketsHttpHandler) behavior, not code XApiSharp.Net owns
/// (HTTP-01: the SDK never owns the HttpClient/handler) - the test exists to pin down and
/// document the boundary of what we can rely on rather than to test our own code.
/// </summary>
public class CrossOriginRedirectTests
{
    [Fact]
    public async Task Cross_origin_redirect_does_not_forward_the_Authorization_header()
    {
        var targetPort = GetFreePort();
        var sourcePort = GetFreePort();

        using var targetListener = new HttpListener();
        targetListener.Prefixes.Add($"http://localhost:{targetPort}/");
        targetListener.Start();

        using var sourceListener = new HttpListener();
        sourceListener.Prefixes.Add($"http://localhost:{sourcePort}/");
        sourceListener.Start();

        string? authorizationSeenByTarget = null;

        var targetTask = Task.Run(async () =>
        {
            var context = await targetListener.GetContextAsync();
            authorizationSeenByTarget = context.Request.Headers["Authorization"];
            var body = Encoding.UTF8.GetBytes("""{"data":{"id":"1","name":"A","username":"a"}}""");
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body);
            context.Response.Close();
        });

        var sourceTask = Task.Run(async () =>
        {
            var context = await sourceListener.GetContextAsync();
            context.Response.StatusCode = 302;
            context.Response.RedirectLocation = $"http://localhost:{targetPort}/2/users/1";
            context.Response.Close();
        });

        using var httpClient = new HttpClient();
        var options = new XClientOptions { BaseUrl = new Uri($"http://localhost:{sourcePort}") };
        var client = new XApiClient(httpClient, new BearerTokenAuthenticationProvider("secret-token"), options);

        var response = await client.Users.GetByIdAsync(new GetUserRequest { Id = "1" });

        await Task.WhenAll(targetTask, sourceTask).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("A", response.Body?.Data?.Name);
        Assert.True(
            string.IsNullOrEmpty(authorizationSeenByTarget),
            $"Authorization leaked to the redirect target: '{authorizationSeenByTarget}'");
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

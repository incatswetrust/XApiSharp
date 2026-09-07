using System.Net;
using System.Text;
using XApiSharp.Authentication;
using XApiSharp.Transport;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>SER-10, end to end through RequestExecutor (not just the QueryStringBuilder unit in
/// isolation) - exercised directly since no public endpoint takes query parameters yet (E4).</summary>
public class RequestExecutorQueryParameterTests
{
    [Fact]
    public async Task Query_parameters_are_appended_and_escaped_exactly_once()
    {
        Uri? seenUri = null;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            seenUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{"id":"1","name":"A","username":"a"}}""", Encoding.UTF8, "application/json"),
            });
        });
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        var fields = QueryStringBuilder.JoinCommaSeparated(["id", "name", "username"]);
        await executor.SendAsync<GetUserResponse>(
            HttpMethod.Get,
            "2/users/1",
            [("user.fields", fields), ("note", "a b&c")],
            CancellationToken.None);

        Assert.NotNull(seenUri);
        Assert.Equal("/2/users/1", seenUri!.AbsolutePath);
        Assert.Equal("?user.fields=id%2Cname%2Cusername&note=a%20b%26c", seenUri.Query);
    }

    [Fact]
    public async Task No_query_string_is_appended_when_all_parameters_are_absent()
    {
        Uri? seenUri = null;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            seenUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{"id":"1","name":"A","username":"a"}}""", Encoding.UTF8, "application/json"),
            });
        });
        using var httpClient = new HttpClient(handler);
        var executor = new RequestExecutor(httpClient, new BearerTokenAuthenticationProvider("t"), new XClientOptions(), TimeProvider.System, new Random(1));

        await executor.SendAsync<GetUserResponse>(
            HttpMethod.Get,
            "2/users/1",
            [("user.fields", null)],
            CancellationToken.None);

        Assert.Equal(string.Empty, seenUri!.Query);
    }
}

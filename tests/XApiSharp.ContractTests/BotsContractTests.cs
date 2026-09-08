using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Bots;
using XApiSharp.Common;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the Bots family (6 operations), per spec section 19.3.</summary>
public class BotsContractTests
{
    [Fact]
    public async Task GetAll_sends_no_parameters_by_default_and_deserializes_meta()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/bots", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","name":"Bot","username":"bot"}],"meta":{"max_bots":5,"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Bots.GetAllAsync();

        Assert.Equal(5, response.Body!.Meta!.MaxBots);
    }

    [Fact]
    public async Task Create_sends_handle_and_returns_201_with_a_one_time_token()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/bots", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("mybot12345", doc.RootElement.GetProperty("handle").GetString());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"1","token":"secret-token"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var response = await client.Bots.CreateAsync(new CreateBotRequest { Handle = "mybot12345" });

        Assert.Equal("secret-token", response.Body!.Data!.Token);
    }

    [Fact]
    public async Task Delete_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/bots/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Bots.DeleteAsync(new DeleteBotRequest { Id = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task Update_sends_only_the_fields_that_were_set()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/2/bots/1", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("no_one", doc.RootElement.GetProperty("dm_permission").GetString());
            Assert.False(doc.RootElement.TryGetProperty("handle", out _));
            return SuccessResponse("""{"data":{"updated":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Bots.UpdateAsync(new UpdateBotRequest { Id = "1", DmPermission = XBotDmAccess.NoOne });

        Assert.True(response.Body!.Data!.Updated);
    }

    [Fact]
    public async Task RevokeToken_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/bots/1/token", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"revoked":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Bots.RevokeTokenAsync(new RevokeBotTokenRequest { Id = "1" });

        Assert.True(response.Body!.Data!.Revoked);
    }

    [Fact]
    public async Task RotateToken_sends_scopes_and_returns_a_new_one_time_token()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/bots/1/token", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("tweet.read", doc.RootElement.GetProperty("scopes")[0].GetString());
            return SuccessResponse("""{"data":{"id":"1","token":"new-secret"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Bots.RotateTokenAsync(new RotateBotTokenRequest { Id = "1", Scopes = ["tweet.read"] });

        Assert.Equal("new-secret", response.Body!.Data!.Token);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

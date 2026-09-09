using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Common;
using XApiSharp.Webhooks;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the 8 Webhooks operations plus the 5 Account Activity subscription
/// operations (issue #18), per spec section 19.3.</summary>
public class WebhooksContractTests
{
    [Fact]
    public async Task GetStreamLinksAsync_hits_the_stream_links_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/tweets/search/webhooks", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"webhook_id":"1","instance_id":"2"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.GetStreamLinksAsync(new GetWebhooksStreamLinksRequest());

        Assert.Equal("1", response.Body!.Data![0].WebhookId);
    }

    [Fact]
    public async Task DeleteStreamLinkAsync_substitutes_the_webhook_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/tweets/search/webhooks/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.DeleteStreamLinkAsync(new DeleteWebhooksStreamLinkRequest { WebhookId = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task CreateStreamLinkAsync_substitutes_the_webhook_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/tweets/search/webhooks/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"provisioned":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.CreateStreamLinkAsync(new CreateWebhooksStreamLinkRequest { WebhookId = "1" });

        Assert.True(response.Body!.Data!.Provisioned);
    }

    [Fact]
    public async Task GetAllAsync_sends_the_webhook_config_fields_query_parameter()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/webhooks", request.RequestUri!.AbsolutePath);
            Assert.Contains("webhook_config.fields=valid", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","url":"https://example.com","valid":true}],"meta":{"result_count":1}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.GetAllAsync(new GetWebhooksRequest { Fields = [XWebhookConfigField.Valid] });

        Assert.Equal(1, response.Body!.Meta!.ResultCount);
        Assert.True(response.Body.Data![0].Valid);
    }

    [Fact]
    public async Task CreateAsync_sends_the_url_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/webhooks", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("https://example.com/hook", doc.RootElement.GetProperty("url").GetString());
            return SuccessResponse("""{"data":{"id":"1","url":"https://example.com/hook","valid":false,"created_at":"2026-01-01T00:00:00Z"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.CreateAsync(new CreateWebhooksRequest { Url = "https://example.com/hook" });

        Assert.Equal("1", response.Body!.Data!.Id);
    }

    [Fact]
    public async Task CreateReplayJobAsync_formats_dates_as_12_digit_utc_minutes()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/webhooks/replay", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("1", doc.RootElement.GetProperty("webhook_id").GetString());
            Assert.Equal("202601010930", doc.RootElement.GetProperty("from_date").GetString());
            Assert.Equal("202601021015", doc.RootElement.GetProperty("to_date").GetString());
            return SuccessResponse("""{"data":{"job_id":"9","created_at":"2026-01-01T00:00:00Z"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.CreateReplayJobAsync(new CreateWebhookReplayJobRequest
        {
            WebhookId = "1",
            FromDate = new DateTimeOffset(2026, 1, 1, 9, 30, 0, TimeSpan.Zero),
            ToDate = new DateTimeOffset(2026, 1, 2, 10, 15, 0, TimeSpan.Zero),
        });

        Assert.Equal("9", response.Body!.Data!.JobId);
    }

    [Fact]
    public async Task DeleteAsync_substitutes_the_webhook_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/webhooks/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.DeleteAsync(new DeleteWebhooksRequest { WebhookId = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task ValidateAsync_sends_a_put_and_substitutes_the_webhook_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/2/webhooks/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"valid":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.ValidateAsync(new ValidateWebhooksRequest { WebhookId = "1" });

        Assert.True(response.Body!.Data!.Valid);
    }

    [Fact]
    public async Task ValidateAccountActivitySubscriptionAsync_substitutes_the_webhook_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/account_activity/webhooks/1/subscriptions/all", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"subscribed":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.ValidateAccountActivitySubscriptionAsync(new ValidateAccountActivitySubscriptionRequest { WebhookId = "1" });

        Assert.True(response.Body!.Data!.Subscribed);
    }

    [Fact]
    public async Task CreateAccountActivitySubscriptionAsync_sends_no_body()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/account_activity/webhooks/1/subscriptions/all", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"subscribed":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.CreateAccountActivitySubscriptionAsync(new CreateAccountActivitySubscriptionRequest { WebhookId = "1" });

        Assert.True(response.Body!.Data!.Subscribed);
    }

    [Fact]
    public async Task GetAccountActivitySubscriptionsAsync_substitutes_the_webhook_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/account_activity/webhooks/1/subscriptions/all/list", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"webhook_id":"1","subscriptions":[{"user_id":"9"}]}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.GetAccountActivitySubscriptionsAsync(new GetAccountActivitySubscriptionsRequest { WebhookId = "1" });

        Assert.Equal("9", response.Body!.Data!.Subscriptions![0].UserId);
    }

    [Fact]
    public async Task DeleteAccountActivitySubscriptionAsync_substitutes_webhook_and_user_id_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/account_activity/webhooks/1/subscriptions/9/all", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"subscribed":false}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.DeleteAccountActivitySubscriptionAsync(new DeleteAccountActivitySubscriptionRequest { WebhookId = "1", UserId = "9" });

        Assert.False(response.Body!.Data!.Subscribed);
    }

    [Fact]
    public async Task GetAccountActivitySubscriptionCountAsync_hits_the_count_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/account_activity/subscriptions/count", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"subscriptions_count_all":"42"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Webhooks.GetAccountActivitySubscriptionCountAsync(new GetAccountActivitySubscriptionCountRequest());

        Assert.Equal("42", response.Body!.Data!.SubscriptionsCountAll);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

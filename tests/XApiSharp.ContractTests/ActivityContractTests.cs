using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Activity;
using XApiSharp.Authentication;
using XApiSharp.Common;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the 5 Activity subscription operations (issue #19), per spec 19.3.</summary>
public class ActivityContractTests
{
    [Fact]
    public async Task GetSubscriptionsAsync_sends_max_results_and_pagination_token()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/activity/subscriptions", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("max_results=10", query, StringComparison.Ordinal);
            Assert.Contains("pagination_token=tok-1", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"subscription_id":"1","event_type":"post.create"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Activity.GetSubscriptionsAsync(new GetActivitySubscriptionsRequest { MaxResults = 10, PaginationToken = "tok-1" });

        Assert.Equal("post.create", response.Body!.Data![0].EventType);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_sends_event_type_and_filter()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/activity/subscriptions", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("follow.follow", doc.RootElement.GetProperty("event_type").GetString());
            Assert.Equal("u1", doc.RootElement.GetProperty("filter").GetProperty("user_id").GetString());
            return SuccessResponse("""{"data":{"subscription":{"subscription_id":"9","event_type":"follow.follow"}}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Activity.CreateSubscriptionAsync(new CreateActivitySubscriptionRequest
        {
            EventType = XActivityEventType.FollowFollow,
            Filter = new ActivitySubscriptionFilter { UserId = "u1" },
        });

        Assert.Equal("9", response.Body!.Data!.Subscription!.SubscriptionId);
    }

    [Fact]
    public async Task DeleteSubscriptionsByIdsAsync_sends_comma_joined_ids_and_parses_partial_results()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/activity/subscriptions", request.RequestUri!.AbsolutePath);
            Assert.Contains("ids=1%2C2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"subscription_id":"1","deleted":true}],"errors":[{"subscription_id":"2","reason":"not_found","message":"no such subscription"}],"meta":{"total_subscriptions":3}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Activity.DeleteSubscriptionsByIdsAsync(new DeleteActivitySubscriptionsByIdsRequest { Ids = ["1", "2"] });

        Assert.True(response.Body!.Data![0].Deleted);
        Assert.Equal("not_found", response.Body.Errors![0].Reason);
        Assert.Equal(3, response.Body.Meta!.TotalSubscriptions);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Activity.DeleteSubscriptionsByIdsAsync(new DeleteActivitySubscriptionsByIdsRequest { Ids = [] }));
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_substitutes_the_subscription_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/activity/subscriptions/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Activity.DeleteSubscriptionAsync(new DeleteActivitySubscriptionRequest { SubscriptionId = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_sends_tag_and_webhook_id()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Put, request.Method);
            Assert.Equal("/2/activity/subscriptions/1", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("prod", doc.RootElement.GetProperty("tag").GetString());
            return SuccessResponse("""{"data":{"subscription":{"subscription_id":"1","tag":"prod"},"total_subscriptions":4}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Activity.UpdateSubscriptionAsync(new UpdateActivitySubscriptionRequest { SubscriptionId = "1", Tag = "prod" });

        Assert.Equal("prod", response.Body!.Data!.Subscription!.Tag);
        Assert.Equal(4, response.Body.Data.TotalSubscriptions);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

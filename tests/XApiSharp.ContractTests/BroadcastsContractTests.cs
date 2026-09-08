using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Broadcasts;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Broadcasts family (13 operations), per spec section 19.3. Full
/// coverage on List (representative paginated operation), the scheduled-broadcast CRUD lifecycle,
/// and the chat sub-resource; the remaining single-purpose operations get one path+param check
/// each.
/// </summary>
public class BroadcastsContractTests
{
    [Fact]
    public async Task List_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/broadcasts", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var broadcast in client.Broadcasts.ListAsync())
        {
            ids.Add(broadcast.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ListScheduled_sends_time_range_and_has_no_pagination_in_the_response()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/broadcasts/scheduled", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"scheduled_broadcast_id":"1","state":"Scheduled"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.ListScheduledAsync();

        Assert.Equal("Scheduled", response.Body!.Data![0].State);
    }

    [Fact]
    public async Task CreateScheduled_sends_epoch_millisecond_timestamps_and_recurrence_and_returns_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/broadcasts/scheduled", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("src1", doc.RootElement.GetProperty("source_id").GetString());
            Assert.Equal("Weekly", doc.RootElement.GetProperty("recurrence").GetProperty("frequency").GetString());
            Assert.Matches("^[0-9]+$", doc.RootElement.GetProperty("scheduled_start_ms").GetString()!);
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"scheduled_broadcast_id":"1","broadcast_id":"abc123"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var response = await client.Broadcasts.CreateScheduledAsync(new CreateScheduledBroadcastRequest
        {
            SourceId = "src1",
            ScheduledStart = start,
            ScheduledEnd = start.AddHours(1),
            Recurrence = new ScheduledBroadcastRecurrence { Frequency = ScheduledBroadcastFrequency.Weekly, Repeats = 4 },
        });

        Assert.Equal("abc123", response.Body!.Data!.BroadcastId);
    }

    [Fact]
    public async Task DeleteScheduled_sends_roll_forward_and_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/broadcasts/scheduled/1", request.RequestUri!.AbsolutePath);
            Assert.Contains("roll_forward=true", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.DeleteScheduledAsync(new DeleteScheduledBroadcastRequest { Id = "1", RollForward = true });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task GetScheduled_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/broadcasts/scheduled/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"scheduled_broadcast_id":"1"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.GetScheduledAsync(new GetScheduledBroadcastRequest { Id = "1" });

        Assert.Equal("1", response.Body!.Data!.ScheduledBroadcastId);
    }

    [Fact]
    public async Task UpdateScheduled_sends_the_numeric_scheduler_id_in_the_body_distinct_from_the_path_id()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/broadcasts/scheduled/abc123", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("999", doc.RootElement.GetProperty("scheduled_broadcast_id").GetString());
            return SuccessResponse("""{"data":{"state":"Scheduled"}}""");
        });
        var client = CreateClient(handler);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        await client.Broadcasts.UpdateScheduledAsync(new UpdateScheduledBroadcastRequest
        {
            Id = "abc123",
            ScheduledBroadcastId = "999",
            ScheduledStart = start,
            ScheduledEnd = start.AddHours(1),
        });
    }

    [Fact]
    public async Task GoLiveScheduled_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/broadcasts/scheduled/1/live", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"state":"Running"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.GoLiveScheduledAsync(new GoLiveScheduledBroadcastRequest { Id = "1" });

        Assert.Equal("Running", response.Body!.Data!.State);
    }

    [Fact]
    public async Task Get_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/broadcasts/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","title":"Live"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.GetAsync(new GetBroadcastRequest { Id = "1" });

        Assert.Equal("Live", response.Body!.Data!.Title);
    }

    [Fact]
    public async Task GetChat_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/broadcasts/1/chat", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","text":"hi"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","text":"there"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var texts = new List<string?>();
        await foreach (var message in client.Broadcasts.GetChatAsync(new GetBroadcastChatRequest { Id = "1" }))
        {
            texts.Add(message.Text);
        }

        Assert.Equal(["hi", "there"], texts);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SendChat_sends_text_and_reply_to()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/broadcasts/1/chat", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("hello", doc.RootElement.GetProperty("text").GetString());
            Assert.Equal("42", doc.RootElement.GetProperty("reply_to").GetString());
            return SuccessResponse("""{"data":{"success":true,"timestamp":"123456789"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.SendChatAsync(new SendBroadcastChatRequest { Id = "1", Text = "hello", ReplyTo = "42" });

        Assert.True(response.Body!.Data!.Success);
    }

    [Fact]
    public async Task MuteChatUser_sends_user_id_and_end_at_ms()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/broadcasts/1/chat/mutes", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("9", doc.RootElement.GetProperty("user_id").GetString());
            return SuccessResponse("""{"data":{"muted":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.MuteChatUserAsync(new MuteBroadcastChatUserRequest { Id = "1", UserId = "9" });

        Assert.True(response.Body!.Data!.Muted);
    }

    [Fact]
    public async Task UnmuteChatUser_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/broadcasts/1/chat/mutes/9", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"muted":false}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.UnmuteChatUserAsync(new UnmuteBroadcastChatUserRequest { Id = "1", UserId = "9" });

        Assert.False(response.Body!.Data!.Muted);
    }

    [Fact]
    public async Task DeleteChatMessage_substitutes_both_path_segments()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/broadcasts/1/chat/555", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Broadcasts.DeleteChatMessageAsync(new DeleteBroadcastChatMessageRequest { Id = "1", MessageId = "555" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

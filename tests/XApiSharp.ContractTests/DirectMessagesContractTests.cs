using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.DirectMessages;

namespace XApiSharp.ContractTests;

/// <summary>
/// Contract checks for the Direct Messages family (9 operations, ordinary DMs only per spec
/// section 3.2 - not X Chat), per spec section 19.3.
/// </summary>
public class DirectMessagesContractTests
{
    [Fact]
    public async Task CreateConversation_sends_participant_ids_and_message_and_returns_201()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/dm_conversations", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("Group", doc.RootElement.GetProperty("conversation_type").GetString());
            Assert.Equal(2, doc.RootElement.GetProperty("participant_ids").GetArrayLength());
            Assert.Equal("hi all", doc.RootElement.GetProperty("message").GetProperty("text").GetString());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"dm_conversation_id":"c1","dm_event_id":"e1"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        var response = await client.DirectMessages.CreateConversationAsync(new CreateConversationRequest
        {
            ParticipantIds = ["1", "2"],
            Message = new DirectMessageContent { Text = "hi all" },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("c1", response.Body!.Data!.DmConversationId);
    }

    [Fact]
    public async Task CreateConversation_requires_at_least_one_participant()
    {
        using var handler = new FakeHttpMessageHandler((_, _) => throw new InvalidOperationException("should not send"));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.DirectMessages.CreateConversationAsync(new CreateConversationRequest
        {
            ParticipantIds = [],
            Message = new DirectMessageContent { Text = "x" },
        }));
    }

    [Fact]
    public async Task SendToParticipant_sends_media_attachments()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/dm_conversations/with/9/messages", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.False(doc.RootElement.TryGetProperty("text", out _));
            Assert.Equal("555", doc.RootElement.GetProperty("attachments")[0].GetProperty("media_id").GetString());
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"dm_conversation_id":"c1","dm_event_id":"e1"}}""", Encoding.UTF8, "application/json"),
            };
        });
        var client = CreateClient(handler);

        await client.DirectMessages.SendToParticipantAsync(new SendToParticipantRequest
        {
            ParticipantId = "9",
            Message = new DirectMessageContent { MediaIds = ["555"] },
        });
    }

    [Fact]
    public async Task SendToConversation_substitutes_the_conversation_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/dm_conversations/c1/messages", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"dm_conversation_id":"c1","dm_event_id":"e2"}}""", Encoding.UTF8, "application/json"),
            });
        });
        var client = CreateClient(handler);

        var response = await client.DirectMessages.SendToConversationAsync(new SendToConversationRequest
        {
            DmConversationId = "c1",
            Message = new DirectMessageContent { Text = "reply" },
        });

        Assert.Equal("e2", response.Body!.Data!.DmEventId);
    }

    [Fact]
    public async Task GetEvents_items_traverses_pages_lazily()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/dm_events", request.RequestUri!.AbsolutePath);
            var isFirstCall = !request.RequestUri.Query.Contains("pagination_token=", StringComparison.Ordinal);
            var body = isFirstCall
                ? """{"data":[{"id":"1","event_type":"MessageCreate"}],"meta":{"next_token":"p2"}}"""
                : """{"data":[{"id":"2","event_type":"MessageCreate"}],"meta":{}}""";
            return Task.FromResult(SuccessResponse(body));
        });
        var client = CreateClient(handler);

        var ids = new List<string>();
        await foreach (var evt in client.DirectMessages.GetEventsAsync())
        {
            ids.Add(evt.Id);
        }

        Assert.Equal(["1", "2"], ids);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task GetEventsByConversation_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/dm_conversations/c1/dm_events", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).DirectMessages.GetEventsByConversationPageAsync("c1");
    }

    [Fact]
    public async Task GetEventsByParticipant_sends_correct_path()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/dm_conversations/with/9/dm_events", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[]}"""));
        });
        await CreateClient(handler).DirectMessages.GetEventsByParticipantPageAsync("9");
    }

    [Fact]
    public async Task GetEventById_substitutes_the_event_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/dm_events/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"1","text":"hi"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.DirectMessages.GetEventByIdAsync(new GetDmEventByIdRequest { EventId = "1" });

        Assert.Equal("hi", response.Body!.Data!.Text);
    }

    [Fact]
    public async Task DeleteEvent_substitutes_the_event_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/2/dm_events/1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"deleted":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.DirectMessages.DeleteEventAsync(new DeleteDmEventRequest { EventId = "1" });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task DownloadMedia_substitutes_all_three_path_segments_and_returns_raw_bytes()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/2/dm_conversations/media/1/2/3", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expectedBytes) { Headers = { ContentType = new("application/octet-stream") } },
            });
        });
        var client = CreateClient(handler);

        var response = await client.DirectMessages.DownloadMediaAsync(new DownloadMediaRequest { DmId = "1", MediaId = "2", ResourceId = "3" });

        Assert.Equal(expectedBytes, response.Body);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

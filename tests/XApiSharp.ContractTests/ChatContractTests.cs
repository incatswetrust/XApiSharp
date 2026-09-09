using System.Net;
using System.Text;
using System.Text.Json;
using XApiSharp.Authentication;
using XApiSharp.Chat;
using XApiSharp.Common;

namespace XApiSharp.ContractTests;

/// <summary>Contract checks for the 18 X Chat HTTP operations, per spec section 19.3.</summary>
public class ChatContractTests
{
    [Fact]
    public async Task GetConversationsPageAsync_sends_pagination_and_field_params()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/chat/conversations", request.RequestUri!.AbsolutePath);
            var query = request.RequestUri.Query;
            Assert.Contains("chat_conversation.fields=group_name", query, StringComparison.Ordinal);
            Assert.Contains("expansions=member_ids", query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"1","group_name":"Team"}],"meta":{"next_token":"tok-2"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.GetConversationsPageAsync(new GetChatConversationsRequest
        {
            Fields = [XChatConversationField.GroupName],
            Expansions = [XChatExpansion.MemberIds],
        });

        Assert.Equal("Team", response.Body!.Data![0].GroupName);
        Assert.Equal("tok-2", response.Body.Meta!.NextToken);
    }

    [Fact]
    public async Task GetConversationsAsync_pages_across_multiple_calls_via_next_token()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                Assert.DoesNotContain("pagination_token", request.RequestUri!.Query, StringComparison.Ordinal);
                return Task.FromResult(SuccessResponse("""{"data":[{"id":"1"}],"meta":{"next_token":"tok-2"}}"""));
            }

            Assert.Contains("pagination_token=tok-2", request.RequestUri!.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"2"}]}"""));
        });
        var client = CreateClient(handler);

        var conversations = new List<ChatConversation>();
        await foreach (var c in client.Chat.GetConversationsAsync(new GetChatConversationsRequest()))
        {
            conversations.Add(c);
        }

        Assert.Equal(["1", "2"], conversations.Select(c => c.Id));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task CreateConversationAsync_sends_participant_keys_and_action_signatures()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/conversations/group", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("conv-1", doc.RootElement.GetProperty("conversation_id").GetString());
            Assert.Equal("u2", doc.RootElement.GetProperty("conversation_participant_keys")[0].GetProperty("user_id").GetString());
            Assert.Equal("sig-1", doc.RootElement.GetProperty("action_signatures")[0].GetProperty("message_event_signature").GetProperty("signature").GetString());
            return SuccessResponse("""{"data":{"conversation_id":"conv-1"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.CreateConversationAsync(new CreateChatConversationRequest
        {
            ConversationId = "conv-1",
            ConversationKeyVersion = "1",
            ConversationParticipantKeys = [new ChatConversationParticipantKey { UserId = "u2", EncryptedConversationKey = "enc-key" }],
            GroupMembers = ["u1", "u2"],
            ActionSignatures =
            [
                new ChatActionSignature
                {
                    MessageId = "m1",
                    EncodedMessageEventDetail = "detail",
                    MessageEventSignature = new ChatMessageEventSignature
                    {
                        Signature = "sig-1",
                        PublicKeyVersion = "1",
                        SignatureVersion = "1",
                    },
                },
            ],
        });

        Assert.Equal("conv-1", response.Body!.Data!.ConversationId);
    }

    [Fact]
    public async Task InitializeGroupAsync_sends_no_body()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/chat/conversations/group/initialize", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"conversation_id":"conv-9"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.InitializeGroupAsync(new InitializeChatGroupRequest());

        Assert.Equal("conv-9", response.Body!.Data!.ConversationId);
    }

    [Fact]
    public async Task GetConversationAsync_substitutes_the_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"id":"conv-1","type":"GROUP_DM"}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.GetConversationAsync(new GetChatConversationRequest { Id = "conv-1" });

        Assert.Equal("GROUP_DM", response.Body!.Data!.Type);
    }

    [Fact]
    public async Task GetConversationEventsPageAsync_sends_the_conversation_id_path_and_fields()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1/events", request.RequestUri!.AbsolutePath);
            Assert.Contains("chat_message_event.fields=encoded_event", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"e1","encoded_event":"base64=="}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.GetConversationEventsPageAsync(new GetChatConversationEventsRequest
        {
            ConversationId = "conv-1",
            Fields = [XChatMessageEventField.EncodedEvent],
        });

        Assert.Equal("base64==", response.Body!.Data![0].EncodedEvent);
    }

    [Fact]
    public async Task GetConversationEventsAsync_pages_across_multiple_calls()
    {
        var callCount = 0;
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            callCount++;
            Assert.Equal("/2/chat/conversations/conv-1/events", request.RequestUri!.AbsolutePath);
            if (callCount == 1)
            {
                return Task.FromResult(SuccessResponse("""{"data":[{"id":"e1"}],"meta":{"next_token":"tok-2"}}"""));
            }

            Assert.Contains("pagination_token=tok-2", request.RequestUri!.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"id":"e2"}]}"""));
        });
        var client = CreateClient(handler);

        var events = new List<ChatMessageEvent>();
        await foreach (var e in client.Chat.GetConversationEventsAsync(new GetChatConversationEventsRequest { ConversationId = "conv-1" }))
        {
            events.Add(e);
        }

        Assert.Equal(["e1", "e2"], events.Select(e => e.Id));
    }

    [Fact]
    public async Task AddConversationKeysAsync_substitutes_the_conversation_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1/keys", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("2", doc.RootElement.GetProperty("conversation_key_version").GetString());
            return SuccessResponse("""{"data":{"conversation_id":"conv-1","sequence_id":"5"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.AddConversationKeysAsync(new AddConversationKeysRequest
        {
            ConversationId = "conv-1",
            ConversationKeyVersion = "2",
            ConversationParticipantKeys = [new ChatConversationParticipantKey { UserId = "u1" }],
        });

        Assert.Equal("5", response.Body!.Data!.SequenceId);
    }

    [Fact]
    public async Task AddGroupMembersAsync_sends_user_ids_and_rejects_an_empty_list()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1/members", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("u3", doc.RootElement.GetProperty("user_ids")[0].GetString());
            return SuccessResponse("""{"data":{"id":"1"}}""");
        });
        var client = CreateClient(handler);

        await client.Chat.AddGroupMembersAsync(new AddChatGroupMembersRequest { ConversationId = "conv-1", UserIds = ["u3"] });

        await Assert.ThrowsAsync<ArgumentException>(() => client.Chat.AddGroupMembersAsync(new AddChatGroupMembersRequest { ConversationId = "conv-1", UserIds = [] }));
    }

    [Fact]
    public async Task SendMessageAsync_sends_the_encoded_event_and_message_id()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1/messages", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("m1", doc.RootElement.GetProperty("message_id").GetString());
            Assert.Equal("encoded==", doc.RootElement.GetProperty("encoded_message_create_event").GetString());
            return SuccessResponse("""{"data":{"encoded_message_event":"reply=="}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.SendMessageAsync(new SendChatMessageRequest
        {
            ConversationId = "conv-1",
            MessageId = "m1",
            EncodedMessageCreateEvent = "encoded==",
        });

        Assert.Equal("reply==", response.Body!.Data!.EncodedMessageEvent);
    }

    [Fact]
    public async Task DeleteMessagesAsync_sends_the_delete_action_and_sequence_ids()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1/messages/delete", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("delete_for_all", doc.RootElement.GetProperty("delete_message_action").GetString());
            Assert.Equal("s1", doc.RootElement.GetProperty("sequence_ids")[0].GetString());
            return SuccessResponse("""{"data":{"deleted":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.DeleteMessagesAsync(new DeleteChatMessagesRequest
        {
            ConversationId = "conv-1",
            SequenceIds = ["s1"],
            DeleteMessageAction = XChatDeleteMessageAction.DeleteForAll,
            ActionSignatures =
            [
                new ChatActionSignature
                {
                    MessageId = "m1",
                    EncodedMessageEventDetail = "detail",
                    MessageEventSignature = new ChatMessageEventSignature { Signature = "s", PublicKeyVersion = "1", SignatureVersion = "1" },
                },
            ],
        });

        Assert.True(response.Body!.Data!.Deleted);
    }

    [Fact]
    public async Task MarkConversationReadAsync_sends_the_seen_until_sequence_id()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/conversations/conv-1/read", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("42", doc.RootElement.GetProperty("seen_until_sequence_id").GetString());
            return SuccessResponse("""{"data":{"success":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.MarkConversationReadAsync(new MarkChatConversationReadRequest { ConversationId = "conv-1", SeenUntilSequenceId = "42" });

        Assert.True(response.Body!.Data!.Success);
    }

    [Fact]
    public async Task SendTypingIndicatorAsync_substitutes_the_conversation_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/2/chat/conversations/conv-1/typing", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":{"success":true}}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.SendTypingIndicatorAsync(new SendChatTypingIndicatorRequest { ConversationId = "conv-1" });

        Assert.True(response.Body!.Data!.Success);
    }

    [Fact]
    public async Task UploadMediaInitializeAsync_sends_conversation_id_and_total_bytes()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/media/upload/initialize", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal(1000, doc.RootElement.GetProperty("total_bytes").GetInt64());
            return SuccessResponse("""{"data":{"session_id":"sess-1","media_hash_key":"hash-1","conversation_id":"conv-1"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.UploadMediaInitializeAsync(new ChatMediaUploadInitializeRequest { ConversationId = "conv-1", TotalBytes = 1000 });

        Assert.Equal("sess-1", response.Body!.Data!.SessionId);
    }

    [Fact]
    public async Task UploadMediaAppendAsync_sends_multipart_segment_and_conversation_fields()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/media/upload/sess-1/append", request.RequestUri!.AbsolutePath);
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var parts = new Dictionary<string, byte[]>();
            foreach (var part in multipart)
            {
                parts[part.Headers.ContentDisposition!.Name!.Trim('"')] = await part.ReadAsByteArrayAsync(ct);
            }

            Assert.Equal(new byte[] { 1, 2, 3 }, parts["media"]);
            Assert.Equal("conv-1", Encoding.UTF8.GetString(parts["conversation_id"]));
            Assert.Equal("hash-1", Encoding.UTF8.GetString(parts["media_hash_key"]));
            return SuccessResponse("""{"data":{"expires_at":123}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.UploadMediaAppendAsync(new ChatMediaUploadAppendRequest
        {
            SessionId = "sess-1",
            ConversationId = "conv-1",
            MediaHashKey = "hash-1",
            SegmentIndex = 0,
            Segment = [1, 2, 3],
        });

        Assert.Equal(123, response.Body!.Data!.ExpiresAt);
    }

    [Fact]
    public async Task UploadMediaFinalizeAsync_sends_num_parts_as_a_string()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/chat/media/upload/sess-1/finalize", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("3", doc.RootElement.GetProperty("num_parts").GetString());
            return SuccessResponse("""{"data":{"success":true}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.UploadMediaFinalizeAsync(new ChatMediaUploadFinalizeRequest
        {
            SessionId = "sess-1",
            ConversationId = "conv-1",
            MediaHashKey = "hash-1",
            NumParts = 3,
        });

        Assert.True(response.Body!.Data!.Success);
    }

    [Fact]
    public async Task DownloadMediaAsync_returns_raw_bytes()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/chat/media/conv-1/hash-1", request.RequestUri!.AbsolutePath);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([9, 9, 9]),
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(response);
        });
        var client = CreateClient(handler);

        var response = await client.Chat.DownloadMediaAsync(new ChatMediaDownloadRequest { Id = "conv-1", MediaHashKey = "hash-1" });

        Assert.Equal(new byte[] { 9, 9, 9 }, response.Body);
    }

    [Fact]
    public async Task AddUserPublicKeyAsync_sends_the_nested_public_key_body()
    {
        using var handler = new FakeHttpMessageHandler(async (request, ct) =>
        {
            Assert.Equal("/2/users/1/public_keys", request.RequestUri!.AbsolutePath);
            var json = await request.Content!.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            Assert.Equal("pk-base64", doc.RootElement.GetProperty("public_key").GetProperty("public_key").GetString());
            Assert.Equal("1", doc.RootElement.GetProperty("version").GetString());
            return SuccessResponse("""{"data":{"public_key_version":"1"}}""");
        });
        var client = CreateClient(handler);

        var response = await client.Chat.AddUserPublicKeyAsync(new AddUserPublicKeyRequest { UserId = "1", PublicKey = "pk-base64", Version = "1" });

        Assert.Equal("1", response.Body!.Data!.PublicKeyVersion);
    }

    [Fact]
    public async Task GetUsersPublicKeysAsync_sends_comma_joined_ids_and_requires_at_least_one()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/public_keys", request.RequestUri!.AbsolutePath);
            Assert.Contains("ids=1%2C2", request.RequestUri.Query, StringComparison.Ordinal);
            return Task.FromResult(SuccessResponse("""{"data":[{"public_key":"pk1"},{"public_key":"pk2"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.GetUsersPublicKeysAsync(new GetUsersPublicKeysRequest { Ids = ["1", "2"] });

        Assert.Equal(2, response.Body!.Data!.Count);

        await Assert.ThrowsAsync<ArgumentException>(() => client.Chat.GetUsersPublicKeysAsync(new GetUsersPublicKeysRequest { Ids = [] }));
    }

    [Fact]
    public async Task GetUserPublicKeyAsync_substitutes_the_user_id_path_segment()
    {
        using var handler = new FakeHttpMessageHandler((request, _) =>
        {
            Assert.Equal("/2/users/1/public_keys", request.RequestUri!.AbsolutePath);
            return Task.FromResult(SuccessResponse("""{"data":[{"public_key":"pk1","public_key_version":"1"}]}"""));
        });
        var client = CreateClient(handler);

        var response = await client.Chat.GetUserPublicKeyAsync(new GetUserPublicKeyRequest { UserId = "1" });

        Assert.Equal("pk1", response.Body!.Data![0].Value);
    }

    private static HttpResponseMessage SuccessResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private static XApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler), new BearerTokenAuthenticationProvider("token"));
}

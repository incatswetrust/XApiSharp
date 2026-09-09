using System.Globalization;
using System.Net.Http.Headers;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.Chat;

/// <summary>
/// Typed methods for X Chat's documented v2 HTTP surface (18 operations per the registry: the 15
/// under <c>/2/chat/*</c>, <c>addUserPublicKey</c>, and the 2 <c>public_keys</c> read operations
/// under <c>/2/users/*</c> - all X Chat identity key material per spec section 3.2, not Users
/// family scope). This is transport only: typed request/response contracts that preserve every
/// key, ciphertext, and signature field exactly as the registry declares it. It is <b>not</b> an
/// end-to-end encryption client - no key generation, encryption, decryption, or cryptographic
/// state management happens here or anywhere else in this SDK (spec section 3.2). For a full
/// encrypted-messenger client, see X's own <see href="https://github.com/xdevplatform/chat-xdk">Chat
/// XDK</see>. Regular Direct Messages (<see cref="DirectMessages.DirectMessagesClient"/>) and X
/// Chat are deliberately kept as separate models, not merged into one message type - they are
/// different products with different guarantees.
/// </summary>
public sealed class ChatClient
{
    private readonly RequestExecutor _executor;

    internal ChatClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary><c>GET /2/chat/conversations</c> - one page. Requires OAuth 2.0
    /// (<c>dm.read</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetChatConversationsResponse>> GetConversationsPageAsync(GetChatConversationsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return FetchConversationsPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/chat/conversations</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetChatConversationsResponse>> GetConversationsPagesAsync(GetChatConversationsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumeratePagesAsync<GetChatConversationsResponse>(
            (token, ct) => FetchConversationsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/chat/conversations</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<ChatConversation> GetConversationsAsync(GetChatConversationsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumerateItemsAsync<GetChatConversationsResponse, ChatConversation>(
            (token, ct) => FetchConversationsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/group</c> - Create Chat Group Conversation.
    /// Requires OAuth 2.0 (<c>users.read</c> + <c>tweet.read</c> + <c>dm.write</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<CreateChatConversationResponse>> CreateConversationAsync(CreateChatConversationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationKeyVersion);

        var body = new CreateChatConversationBody
        {
            ConversationId = request.ConversationId,
            ConversationKeyVersion = request.ConversationKeyVersion,
            ConversationParticipantKeys = ChatConversationParticipantKeyMapper.ToBody(request.ConversationParticipantKeys) ?? [],
            GroupMembers = request.GroupMembers,
            ActionSignatures = ChatActionSignatureMapper.ToBody(request.ActionSignatures),
            Base64EncodedKeyRotation = request.Base64EncodedKeyRotation,
            GroupAdmins = request.GroupAdmins,
            GroupAvatarUrl = request.GroupAvatarUrl,
            GroupDescription = request.GroupDescription,
            GroupName = request.GroupName,
            TtlMsec = request.TtlMilliseconds?.ToString(CultureInfo.InvariantCulture),
        };

        return _executor.SendAsync<CreateChatConversationResponse>(HttpMethod.Post, "2/chat/conversations/group", body, queryParameters: null, cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/group/initialize</c> - Initialize Chat Group.
    /// Requires OAuth 2.0 <c>dm.write</c> or OAuth 1.0a.</summary>
    public Task<XResponse<InitializeChatGroupResponse>> InitializeGroupAsync(InitializeChatGroupRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _executor.SendAsync<InitializeChatGroupResponse>(HttpMethod.Post, "2/chat/conversations/group/initialize", cancellationToken);
    }

    /// <summary><c>GET /2/chat/conversations/{id}</c> - Get Chat Conversation. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>tweet.read</c> + <c>dm.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetChatConversationResponse>> GetConversationAsync(GetChatConversationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("chat_conversation.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, f => f.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.UserFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetChatConversationResponse>(
            HttpMethod.Get,
            $"2/chat/conversations/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary><c>GET /2/chat/conversations/{id}/events</c> - one page. Requires OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c> + <c>dm.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetChatConversationEventsResponse>> GetConversationEventsPageAsync(GetChatConversationEventsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);

        return FetchConversationEventsPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/chat/conversations/{id}/events</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetChatConversationEventsResponse>> GetConversationEventsPagesAsync(GetChatConversationEventsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);

        return XPaginator.EnumeratePagesAsync<GetChatConversationEventsResponse>(
            (token, ct) => FetchConversationEventsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/chat/conversations/{id}/events</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<ChatMessageEvent> GetConversationEventsAsync(GetChatConversationEventsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);

        return XPaginator.EnumerateItemsAsync<GetChatConversationEventsResponse, ChatMessageEvent>(
            (token, ct) => FetchConversationEventsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/{id}/keys</c> - Add Conversation Keys. Requires
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c> + <c>dm.write</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<AddConversationKeysResponse>> AddConversationKeysAsync(AddConversationKeysRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationKeyVersion);

        var body = new AddConversationKeysBody
        {
            ConversationKeyVersion = request.ConversationKeyVersion,
            ConversationParticipantKeys = ChatConversationParticipantKeyMapper.ToBody(request.ConversationParticipantKeys) ?? [],
            ActionSignatures = ChatActionSignatureMapper.ToBody(request.ActionSignatures),
            Base64EncodedKeyRotation = request.Base64EncodedKeyRotation,
        };

        return _executor.SendAsync<AddConversationKeysResponse>(
            HttpMethod.Post,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/keys",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/{id}/members</c> - Add members to a Chat group
    /// conversation. Requires OAuth 2.0 (<c>tweet.read</c> + <c>dm.write</c> + <c>users.read</c>)
    /// or OAuth 1.0a.</summary>
    public Task<XResponse<AddChatGroupMembersResponse>> AddGroupMembersAsync(AddChatGroupMembersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        if (request.UserIds.Count == 0)
        {
            throw new ArgumentException("At least one user ID is required.", nameof(request));
        }

        var body = new AddChatGroupMembersBody
        {
            UserIds = request.UserIds,
            ActionSignatures = ChatActionSignatureMapper.ToBody(request.ActionSignatures),
            ConversationKeyVersion = request.ConversationKeyVersion,
            ConversationParticipantKeys = ChatConversationParticipantKeyMapper.ToBody(request.ConversationParticipantKeys),
            EncryptedAvatarUrl = request.EncryptedAvatarUrl,
            EncryptedTitle = request.EncryptedTitle,
        };

        return _executor.SendAsync<AddChatGroupMembersResponse>(
            HttpMethod.Post,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/members",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/{id}/messages</c> - Send Chat Message. Requires
    /// OAuth 2.0 (<c>tweet.read</c> + <c>dm.write</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<SendChatMessageResponse>> SendMessageAsync(SendChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EncodedMessageCreateEvent);

        var body = new SendChatMessageBody
        {
            MessageId = request.MessageId,
            EncodedMessageCreateEvent = request.EncodedMessageCreateEvent,
            ConversationToken = request.ConversationToken,
            EncodedMessageEventSignature = request.EncodedMessageEventSignature,
        };

        return _executor.SendAsync<SendChatMessageResponse>(
            HttpMethod.Post,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/messages",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/{id}/messages/delete</c> - Delete Chat messages.
    /// Requires OAuth 2.0 (<c>users.read</c> + <c>dm.write</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<DeleteChatMessagesResponse>> DeleteMessagesAsync(DeleteChatMessagesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        if (request.SequenceIds.Count == 0)
        {
            throw new ArgumentException("At least one sequence ID is required.", nameof(request));
        }

        var body = new DeleteChatMessagesBody
        {
            SequenceIds = request.SequenceIds,
            DeleteMessageAction = request.DeleteMessageAction.ToApiValue(),
            ActionSignatures = ChatActionSignatureMapper.ToBody(request.ActionSignatures) ?? [],
            MediaHashKeys = request.MediaHashKeys,
        };

        return _executor.SendAsync<DeleteChatMessagesResponse>(
            HttpMethod.Post,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/messages/delete",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/{id}/read</c> - Mark Conversation as Read. Requires
    /// OAuth 2.0 (<c>tweet.read</c> + <c>dm.write</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<MarkChatConversationReadResponse>> MarkConversationReadAsync(MarkChatConversationReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SeenUntilSequenceId);

        var body = new MarkChatConversationReadBody { SeenUntilSequenceId = request.SeenUntilSequenceId };

        return _executor.SendAsync<MarkChatConversationReadResponse>(
            HttpMethod.Post,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/read",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/conversations/{id}/typing</c> - Send Typing Indicator. Requires
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c> + <c>dm.write</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<SendChatTypingIndicatorResponse>> SendTypingIndicatorAsync(SendChatTypingIndicatorRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);

        return _executor.SendAsync<SendChatTypingIndicatorResponse>(
            HttpMethod.Post,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/typing",
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/media/upload/initialize</c> - Initialize Chat Media Upload.
    /// Requires OAuth 2.0 <c>media.write</c> or OAuth 1.0a.</summary>
    public Task<XResponse<ChatMediaUploadInitializeResponse>> UploadMediaInitializeAsync(ChatMediaUploadInitializeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        if (request.TotalBytes < 0)
        {
            throw new ArgumentException("TotalBytes cannot be negative.", nameof(request));
        }

        var body = new ChatMediaUploadInitializeBody { ConversationId = request.ConversationId, TotalBytes = request.TotalBytes };

        return _executor.SendAsync<ChatMediaUploadInitializeResponse>(HttpMethod.Post, "2/chat/media/upload/initialize", body, queryParameters: null, cancellationToken);
    }

    /// <summary><c>POST /2/chat/media/upload/{id}/append</c> - Append Chat Media Upload. Requires
    /// OAuth 2.0 <c>media.write</c> or OAuth 1.0a.</summary>
    public Task<XResponse<ChatMediaUploadAppendResponse>> UploadMediaAppendAsync(ChatMediaUploadAppendRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaHashKey);
        ArgumentNullException.ThrowIfNull(request.Segment);
        if (request.SegmentIndex is < 0 or > 999)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.SegmentIndex, "SegmentIndex must be 0-999.");
        }

        return _executor.SendMultipartAsync<ChatMediaUploadAppendResponse>(
            HttpMethod.Post,
            $"2/chat/media/upload/{Uri.EscapeDataString(request.SessionId)}/append",
            () =>
            {
                var content = new MultipartFormDataContent();
                var mediaContent = new ByteArrayContent(request.Segment);
                mediaContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(mediaContent, "media", "segment");
                content.Add(new StringContent(request.SegmentIndex.ToString(CultureInfo.InvariantCulture)), "segment_index");
                content.Add(new StringContent(request.ConversationId), "conversation_id");
                content.Add(new StringContent(request.MediaHashKey), "media_hash_key");

                return content;
            },
            cancellationToken);
    }

    /// <summary><c>POST /2/chat/media/upload/{id}/finalize</c> - Finalize Chat Media Upload.
    /// Requires OAuth 2.0 <c>media.write</c> or OAuth 1.0a.</summary>
    public Task<XResponse<ChatMediaUploadFinalizeResponse>> UploadMediaFinalizeAsync(ChatMediaUploadFinalizeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaHashKey);

        var body = new ChatMediaUploadFinalizeBody
        {
            ConversationId = request.ConversationId,
            MediaHashKey = request.MediaHashKey,
            NumParts = request.NumParts.ToString(CultureInfo.InvariantCulture),
            MessageId = request.MessageId,
            TtlMsec = request.TtlMilliseconds?.ToString(CultureInfo.InvariantCulture),
        };

        return _executor.SendAsync<ChatMediaUploadFinalizeResponse>(
            HttpMethod.Post,
            $"2/chat/media/upload/{Uri.EscapeDataString(request.SessionId)}/finalize",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/chat/media/{id}/{media_hash_key}</c> - Download Chat Media. Requires
    /// OAuth 2.0 <c>media.write</c> or OAuth 1.0a. Not JSON (spec section 9) - returns raw bytes.</summary>
    public Task<XResponse<byte[]>> DownloadMediaAsync(ChatMediaDownloadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaHashKey);

        return _executor.SendForBytesAsync(
            HttpMethod.Get,
            $"2/chat/media/{Uri.EscapeDataString(request.Id)}/{Uri.EscapeDataString(request.MediaHashKey)}",
            cancellationToken);
    }

    /// <summary><c>POST /2/users/{id}/public_keys</c> - Add public key. Requires OAuth 2.0
    /// (<c>users.read</c> + <c>dm.write</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<AddUserPublicKeyResponse>> AddUserPublicKeyAsync(AddUserPublicKeyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PublicKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Version);

        var body = new AddUserPublicKeyBody
        {
            Version = request.Version,
            GenerateVersion = request.GenerateVersion,
            PublicKey = new AddUserPublicKeyPublicKeyBody
            {
                PublicKey = request.PublicKey,
                IdentityPublicKeySignature = request.IdentityPublicKeySignature,
                PublicKeyFingerprint = request.PublicKeyFingerprint,
                RegistrationMethod = request.RegistrationMethod,
                SigningPublicKey = request.SigningPublicKey,
                SigningPublicKeySignature = request.SigningPublicKeySignature,
            },
        };

        return _executor.SendAsync<AddUserPublicKeyResponse>(
            HttpMethod.Post,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/public_keys",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/users/public_keys</c> - Get public keys (bulk, up to 100 IDs). Requires
    /// OAuth 2.0 (<c>users.read</c> + <c>dm.read</c> + <c>tweet.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetUsersPublicKeysResponse>> GetUsersPublicKeysAsync(GetUsersPublicKeysRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ids.Count == 0)
        {
            throw new ArgumentException("At least one user ID is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
            ("public_key.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetUsersPublicKeysResponse>(HttpMethod.Get, "2/users/public_keys", query, cancellationToken);
    }

    /// <summary><c>GET /2/users/{id}/public_keys</c> - Get public keys for one user. Requires
    /// OAuth 2.0 (<c>tweet.read</c> + <c>users.read</c> + <c>dm.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetUserPublicKeyResponse>> GetUserPublicKeyAsync(GetUserPublicKeyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        var query = new List<(string Name, string? Value)>
        {
            ("public_key.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetUserPublicKeyResponse>(
            HttpMethod.Get,
            $"2/users/{Uri.EscapeDataString(request.UserId)}/public_keys",
            query,
            cancellationToken);
    }

    private Task<XResponse<GetChatConversationsResponse>> FetchConversationsPageAsync(GetChatConversationsRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("chat_conversation.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, f => f.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.UserFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetChatConversationsResponse>(HttpMethod.Get, "2/chat/conversations", query, cancellationToken);
    }

    private Task<XResponse<GetChatConversationEventsResponse>> FetchConversationEventsPageAsync(GetChatConversationEventsRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("chat_message_event.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetChatConversationEventsResponse>(
            HttpMethod.Get,
            $"2/chat/conversations/{Uri.EscapeDataString(request.ConversationId)}/events",
            query,
            cancellationToken);
    }
}

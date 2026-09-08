using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.DirectMessages;

/// <summary>
/// Typed methods for the Direct Messages family (9 operations per the registry) - ordinary DMs
/// only, not X Chat (spec section 3.2's separate boundary; Chat lands in E5 with its own message
/// model, deliberately not unified with this one).
/// </summary>
public sealed class DirectMessagesClient
{
    private readonly RequestExecutor _executor;

    internal DirectMessagesClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>POST /2/dm_conversations</c> - Create DM conversation (group, 2-49 participants).
    /// Requires OAuth 2.0 (<c>dm.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.
    /// Returns HTTP 201.
    /// </summary>
    public Task<XResponse<DirectMessageSendResponse>> CreateConversationAsync(CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ParticipantIds.Count == 0)
        {
            throw new ArgumentException("At least one participant ID is required.", nameof(request));
        }

        var body = new CreateConversationBody
        {
            ParticipantIds = request.ParticipantIds,
            Message = ToBody(request.Message),
        };

        return _executor.SendAsync<DirectMessageSendResponse>(HttpMethod.Post, "2/dm_conversations", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/dm_conversations/with/{participant_id}/messages</c> - sends a message to (and
    /// implicitly creates, if none exists) a 1:1 conversation with the given participant. Requires
    /// OAuth 2.0 (<c>dm.write</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a. Returns
    /// HTTP 201.
    /// </summary>
    public Task<XResponse<DirectMessageSendResponse>> SendToParticipantAsync(SendToParticipantRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ParticipantId);

        return _executor.SendAsync<DirectMessageSendResponse>(
            HttpMethod.Post,
            $"2/dm_conversations/with/{Uri.EscapeDataString(request.ParticipantId)}/messages",
            ToBody(request.Message),
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/dm_conversations/{dm_conversation_id}/messages</c> - sends a message to an
    /// existing conversation. Requires OAuth 2.0
    /// (<c>tweet.read</c> + <c>dm.write</c> + <c>users.read</c>) or OAuth 1.0a. Returns HTTP 201.
    /// </summary>
    public Task<XResponse<DirectMessageSendResponse>> SendToConversationAsync(SendToConversationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DmConversationId);

        return _executor.SendAsync<DirectMessageSendResponse>(
            HttpMethod.Post,
            $"2/dm_conversations/{Uri.EscapeDataString(request.DmConversationId)}/messages",
            ToBody(request.Message),
            queryParameters: null,
            cancellationToken);
    }

    /// <summary><c>GET /2/dm_events</c> - one page, every conversation the authenticated user
    /// participates in. Requires OAuth 2.0 (<c>dm.read</c> + <c>tweet.read</c> + <c>users.read</c>)
    /// or OAuth 1.0a.</summary>
    public Task<XResponse<GetDmEventsResponse>> GetEventsPageAsync(GetDmEventsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new GetDmEventsRequest();

        return FetchEventsPageAsync("2/dm_events", request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/dm_events</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetDmEventsResponse>> GetEventsPagesAsync(GetDmEventsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default) =>
        EnumerateEventsPages("2/dm_events", request ?? new GetDmEventsRequest(), options, cancellationToken);

    /// <summary><c>GET /2/dm_events</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<DmEvent> GetEventsAsync(GetDmEventsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default) =>
        EnumerateEvents("2/dm_events", request ?? new GetDmEventsRequest(), options, cancellationToken);

    /// <summary><c>GET /2/dm_conversations/{id}/dm_events</c> - one page. Requires OAuth 2.0
    /// (<c>tweet.read</c> + <c>users.read</c> + <c>dm.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetDmEventsResponse>> GetEventsByConversationPageAsync(string dmConversationId, GetDmEventsRequest? request = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dmConversationId);
        request ??= new GetDmEventsRequest();

        return FetchEventsPageAsync(ByConversationPath(dmConversationId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/dm_conversations/{id}/dm_events</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetDmEventsResponse>> GetEventsByConversationPagesAsync(string dmConversationId, GetDmEventsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dmConversationId);

        return EnumerateEventsPages(ByConversationPath(dmConversationId), request ?? new GetDmEventsRequest(), options, cancellationToken);
    }

    /// <summary><c>GET /2/dm_conversations/{id}/dm_events</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<DmEvent> GetEventsByConversationAsync(string dmConversationId, GetDmEventsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dmConversationId);

        return EnumerateEvents(ByConversationPath(dmConversationId), request ?? new GetDmEventsRequest(), options, cancellationToken);
    }

    /// <summary><c>GET /2/dm_conversations/with/{participant_id}/dm_events</c> - one page.
    /// Requires OAuth 2.0 (<c>dm.read</c> + <c>tweet.read</c> + <c>users.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<GetDmEventsResponse>> GetEventsByParticipantPageAsync(string participantId, GetDmEventsRequest? request = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);
        request ??= new GetDmEventsRequest();

        return FetchEventsPageAsync(ByParticipantPath(participantId), request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/dm_conversations/with/{participant_id}/dm_events</c> - lazy
    /// page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetDmEventsResponse>> GetEventsByParticipantPagesAsync(string participantId, GetDmEventsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);

        return EnumerateEventsPages(ByParticipantPath(participantId), request ?? new GetDmEventsRequest(), options, cancellationToken);
    }

    /// <summary><c>GET /2/dm_conversations/with/{participant_id}/dm_events</c> - lazy item
    /// traversal.</summary>
    public IAsyncEnumerable<DmEvent> GetEventsByParticipantAsync(string participantId, GetDmEventsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(participantId);

        return EnumerateEvents(ByParticipantPath(participantId), request ?? new GetDmEventsRequest(), options, cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/dm_events/{event_id}</c> - Get Direct Messages Events by ID. Requires OAuth 2.0
    /// (<c>dm.read</c> + <c>users.read</c> + <c>tweet.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetDmEventByIdResponse>> GetEventByIdAsync(GetDmEventByIdRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EventId);

        var query = new List<(string Name, string? Value)>
        {
            ("dm_event.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.UserFields, f => f.ToApiValue())),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(request.PostFields, f => f.ToApiValue())),
            ("media.fields", QueryStringBuilder.JoinCommaSeparated(request.MediaFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetDmEventByIdResponse>(
            HttpMethod.Get,
            $"2/dm_events/{Uri.EscapeDataString(request.EventId)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/dm_events/{event_id}</c> - Delete DM event. Requires OAuth 2.0
    /// (<c>dm.read</c> + <c>dm.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<DeleteDmEventResponse>> DeleteEventAsync(DeleteDmEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EventId);

        return _executor.SendAsync<DeleteDmEventResponse>(
            HttpMethod.Delete,
            $"2/dm_events/{Uri.EscapeDataString(request.EventId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/dm_conversations/media/{dm_id}/{media_id}/{resource_id}</c> - Download DM Media.
    /// Requires OAuth 2.0 <c>dm.read</c> or OAuth 1.0a. Not JSON - the registry declares
    /// <c>application/octet-stream</c>, so this returns raw bytes rather than a typed body (spec
    /// section 9).
    /// </summary>
    public Task<XResponse<byte[]>> DownloadMediaAsync(DownloadMediaRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DmId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MediaId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceId);

        return _executor.SendForBytesAsync(
            HttpMethod.Get,
            $"2/dm_conversations/media/{Uri.EscapeDataString(request.DmId)}/{Uri.EscapeDataString(request.MediaId)}/{Uri.EscapeDataString(request.ResourceId)}",
            cancellationToken);
    }

    private static string ByConversationPath(string dmConversationId) => $"2/dm_conversations/{Uri.EscapeDataString(dmConversationId)}/dm_events";

    private static string ByParticipantPath(string participantId) => $"2/dm_conversations/with/{Uri.EscapeDataString(participantId)}/dm_events";

    private static DirectMessageContentBody ToBody(DirectMessageContent content) => new()
    {
        Text = content.Text,
        Attachments = content.MediaIds?.Select(id => new DirectMessageAttachmentBody { MediaId = id }).ToList(),
    };

    private IAsyncEnumerable<XResponse<GetDmEventsResponse>> EnumerateEventsPages(string path, GetDmEventsRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumeratePagesAsync<GetDmEventsResponse>(
            (token, ct) => FetchEventsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);

    private IAsyncEnumerable<DmEvent> EnumerateEvents(string path, GetDmEventsRequest request, XPaginationOptions? options, CancellationToken cancellationToken) =>
        XPaginator.EnumerateItemsAsync<GetDmEventsResponse, DmEvent>(
            (token, ct) => FetchEventsPageAsync(path, request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);

    private Task<XResponse<GetDmEventsResponse>> FetchEventsPageAsync(string path, GetDmEventsRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("event_types", QueryStringBuilder.JoinCommaSeparated(request.EventTypes, e => e.ToApiValue())),
            ("dm_event.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.UserFields, f => f.ToApiValue())),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(request.PostFields, f => f.ToApiValue())),
            ("media.fields", QueryStringBuilder.JoinCommaSeparated(request.MediaFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetDmEventsResponse>(HttpMethod.Get, path, query, cancellationToken);
    }
}

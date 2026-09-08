using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.Broadcasts;

/// <summary>
/// Typed methods for the Broadcasts family (13 operations per the registry) - X Live broadcast
/// management (scheduling, going live, broadcast chat). Undocumented on docs.x.com at E0
/// inventory time (present in the OpenAPI snapshot only) and confirmed in scope for E4 per
/// project owner decision.
/// </summary>
public sealed class BroadcastsClient
{
    private readonly RequestExecutor _executor;

    internal BroadcastsClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary><c>GET /2/broadcasts</c> - one page. Requires OAuth 2.0 <c>broadcast.read</c> or
    /// OAuth 1.0a.</summary>
    public Task<XResponse<ListBroadcastsResponse>> ListPageAsync(ListBroadcastsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListBroadcastsRequest();

        return FetchListPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/broadcasts</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<ListBroadcastsResponse>> ListPagesAsync(ListBroadcastsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListBroadcastsRequest();

        return XPaginator.EnumeratePagesAsync<ListBroadcastsResponse>(
            (token, ct) => FetchListPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/broadcasts</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Broadcast> ListAsync(ListBroadcastsRequest? request = null, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListBroadcastsRequest();

        return XPaginator.EnumerateItemsAsync<ListBroadcastsResponse, Broadcast>(
            (token, ct) => FetchListPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/broadcasts/scheduled</c> - List scheduled broadcasts. Not paginated - see
    /// <see cref="ListScheduledBroadcastsRequest"/>. Requires OAuth 2.0 <c>broadcast.read</c> or
    /// OAuth 1.0a.
    /// </summary>
    public Task<XResponse<ListScheduledBroadcastsResponse>> ListScheduledAsync(ListScheduledBroadcastsRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ListScheduledBroadcastsRequest();

        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("oldest_start_time", request.OldestStartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("newest_start_time", request.NewestStartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("pagination_token", request.PaginationToken),
        };

        return _executor.SendAsync<ListScheduledBroadcastsResponse>(HttpMethod.Get, "2/broadcasts/scheduled", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/broadcasts/scheduled</c> - Create a scheduled broadcast. Requires OAuth 2.0
    /// (<c>broadcast.read</c> + <c>broadcast.write</c>) or OAuth 1.0a. Returns HTTP 201.
    /// </summary>
    public Task<XResponse<CreateScheduledBroadcastResponse>> CreateScheduledAsync(CreateScheduledBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SourceId);

        var body = new CreateScheduledBroadcastBody
        {
            SourceId = request.SourceId,
            ScheduledStartMs = ToEpochMs(request.ScheduledStart),
            ScheduledEndMs = ToEpochMs(request.ScheduledEnd),
            Title = request.Title,
            Locale = request.Locale,
            ChatOption = request.ChatOption,
            TelecastId = request.TelecastId,
            ThumbnailMediaId = request.ThumbnailMediaId,
            AvailableForReplay = request.AvailableForReplay,
            IsLocked = request.IsLocked,
            ManualPublish = request.ManualPublish,
            Recurrence = request.Recurrence is { } recurrence
                ? new ScheduledBroadcastRecurrenceBody
                {
                    Frequency = recurrence.Frequency.ToApiValue(),
                    Repeats = recurrence.Repeats.ToString(CultureInfo.InvariantCulture),
                }
                : null,
        };

        return _executor.SendAsync<CreateScheduledBroadcastResponse>(HttpMethod.Post, "2/broadcasts/scheduled", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/broadcasts/scheduled/{id}</c> - Delete a scheduled broadcast. Requires OAuth
    /// 2.0 (<c>broadcast.read</c> + <c>broadcast.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<DeleteScheduledBroadcastResponse>> DeleteScheduledAsync(DeleteScheduledBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("roll_forward", request.RollForward?.ToString(CultureInfo.InvariantCulture).ToLowerInvariant()),
        };

        return _executor.SendAsync<DeleteScheduledBroadcastResponse>(
            HttpMethod.Delete,
            $"2/broadcasts/scheduled/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/broadcasts/scheduled/{id}</c> - Get a scheduled broadcast. Requires OAuth 2.0
    /// <c>broadcast.read</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetScheduledBroadcastResponse>> GetScheduledAsync(GetScheduledBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<GetScheduledBroadcastResponse>(
            HttpMethod.Get,
            $"2/broadcasts/scheduled/{Uri.EscapeDataString(request.Id)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>PUT /2/broadcasts/scheduled/{id}</c> - Update a scheduled broadcast. Requires OAuth 2.0
    /// (<c>broadcast.write</c> + <c>broadcast.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UpdateScheduledBroadcastResponse>> UpdateScheduledAsync(UpdateScheduledBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScheduledBroadcastId);

        var body = new UpdateScheduledBroadcastBody
        {
            ScheduledBroadcastId = request.ScheduledBroadcastId,
            ScheduledStartMs = ToEpochMs(request.ScheduledStart),
            ScheduledEndMs = ToEpochMs(request.ScheduledEnd),
            Title = request.Title,
            Locale = request.Locale,
            ChatOption = request.ChatOption,
            SourceId = request.SourceId,
            ThumbnailMediaId = request.ThumbnailMediaId,
            AvailableForReplay = request.AvailableForReplay,
            IsLocked = request.IsLocked,
            ManualPublish = request.ManualPublish,
            RollForward = request.RollForward,
        };

        return _executor.SendAsync<UpdateScheduledBroadcastResponse>(
            HttpMethod.Put,
            $"2/broadcasts/scheduled/{Uri.EscapeDataString(request.Id)}",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/broadcasts/scheduled/{id}/live</c> - Go live on a scheduled broadcast.
    /// Requires OAuth 2.0 (<c>broadcast.read</c> + <c>broadcast.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GoLiveScheduledBroadcastResponse>> GoLiveScheduledAsync(GoLiveScheduledBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<GoLiveScheduledBroadcastResponse>(
            HttpMethod.Post,
            $"2/broadcasts/scheduled/{Uri.EscapeDataString(request.Id)}/live",
            cancellationToken);
    }

    /// <summary>
    /// <c>GET /2/broadcasts/{id}</c> - Get a broadcast. Requires OAuth 2.0 <c>broadcast.read</c>
    /// or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<GetBroadcastResponse>> GetAsync(GetBroadcastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        var query = new List<(string Name, string? Value)>
        {
            ("broadcast.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetBroadcastResponse>(
            HttpMethod.Get,
            $"2/broadcasts/{Uri.EscapeDataString(request.Id)}",
            query,
            cancellationToken);
    }

    /// <summary><c>GET /2/broadcasts/{id}/chat</c> - one page. Requires OAuth 2.0
    /// <c>broadcast.read</c> or OAuth 1.0a.</summary>
    public Task<XResponse<GetBroadcastChatResponse>> GetChatPageAsync(GetBroadcastChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return FetchChatPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/broadcasts/{id}/chat</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetBroadcastChatResponse>> GetChatPagesAsync(GetBroadcastChatRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return XPaginator.EnumeratePagesAsync<GetBroadcastChatResponse>(
            (token, ct) => FetchChatPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/broadcasts/{id}/chat</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<BroadcastChatMessage> GetChatAsync(GetBroadcastChatRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return XPaginator.EnumerateItemsAsync<GetBroadcastChatResponse, BroadcastChatMessage>(
            (token, ct) => FetchChatPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/broadcasts/{id}/chat</c> - Send a chat message to a live broadcast. Requires
    /// OAuth 2.0 (<c>broadcast.read</c> + <c>broadcast.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<SendBroadcastChatResponse>> SendChatAsync(SendBroadcastChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Text);

        return _executor.SendAsync<SendBroadcastChatResponse>(
            HttpMethod.Post,
            $"2/broadcasts/{Uri.EscapeDataString(request.Id)}/chat",
            new SendBroadcastChatBody { Text = request.Text, ReplyTo = request.ReplyTo },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/broadcasts/{id}/chat/mutes</c> - Mute or time out a user in a broadcast chat.
    /// Requires OAuth 2.0 (<c>broadcast.read</c> + <c>broadcast.write</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<MuteBroadcastChatUserResponse>> MuteChatUserAsync(MuteBroadcastChatUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        var body = new MuteBroadcastChatUserBody { UserId = request.UserId, EndAtMs = request.EndAtMs, MessageId = request.MessageId };

        return _executor.SendAsync<MuteBroadcastChatUserResponse>(
            HttpMethod.Post,
            $"2/broadcasts/{Uri.EscapeDataString(request.Id)}/chat/mutes",
            body,
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/broadcasts/{id}/chat/mutes/{user_id}</c> - Unmute a user in a broadcast chat.
    /// Requires OAuth 2.0 (<c>broadcast.write</c> + <c>broadcast.read</c>) or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<UnmuteBroadcastChatUserResponse>> UnmuteChatUserAsync(UnmuteBroadcastChatUserRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return _executor.SendAsync<UnmuteBroadcastChatUserResponse>(
            HttpMethod.Delete,
            $"2/broadcasts/{Uri.EscapeDataString(request.Id)}/chat/mutes/{Uri.EscapeDataString(request.UserId)}",
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/broadcasts/{id}/chat/{message_id}</c> - Remove a chat message from a live
    /// broadcast. Requires OAuth 2.0 (<c>broadcast.write</c> + <c>broadcast.read</c>) or OAuth
    /// 1.0a.
    /// </summary>
    public Task<XResponse<DeleteBroadcastChatMessageResponse>> DeleteChatMessageAsync(DeleteBroadcastChatMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.MessageId);

        return _executor.SendAsync<DeleteBroadcastChatMessageResponse>(
            HttpMethod.Delete,
            $"2/broadcasts/{Uri.EscapeDataString(request.Id)}/chat/{Uri.EscapeDataString(request.MessageId)}",
            cancellationToken);
    }

    private Task<XResponse<ListBroadcastsResponse>> FetchListPageAsync(ListBroadcastsRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("broadcast.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<ListBroadcastsResponse>(HttpMethod.Get, "2/broadcasts", query, cancellationToken);
    }

    private Task<XResponse<GetBroadcastChatResponse>> FetchChatPageAsync(GetBroadcastChatRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("broadcast_chat_message.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Expansions, e => e.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.UserFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetBroadcastChatResponse>(
            HttpMethod.Get,
            $"2/broadcasts/{Uri.EscapeDataString(request.Id)}/chat",
            query,
            cancellationToken);
    }

    private static string ToEpochMs(DateTimeOffset value) => value.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
}

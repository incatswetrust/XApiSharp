using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.CommunityNotes;

/// <summary>Typed methods for the Community Notes family (5 operations per the registry).</summary>
public sealed class CommunityNotesClient
{
    private readonly RequestExecutor _executor;

    internal CommunityNotesClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>POST /2/notes</c> - Create Community Notes. Requires OAuth 2.0 <c>tweet.write</c> or
    /// OAuth 1.0a. Returns HTTP 201.
    /// </summary>
    public Task<XResponse<CreateNoteResponse>> CreateAsync(CreateNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);
        ArgumentNullException.ThrowIfNull(request.Info);

        var body = new CreateNoteBody
        {
            PostId = request.PostId,
            TestMode = request.TestMode,
            Info = new CreateNoteInfoBody
            {
                Text = request.Info.Text,
                Classification = request.Info.Classification.ToApiValue(),
                TrustworthySources = request.Info.TrustworthySources,
                MisleadingTags = request.Info.MisleadingTags?.Select(t => t.ToApiValue()).ToList(),
                IsMediaNote = request.Info.IsMediaNote,
            },
        };

        return _executor.SendAsync<CreateNoteResponse>(HttpMethod.Post, "2/notes", body, queryParameters: null, cancellationToken);
    }

    /// <summary>
    /// <c>POST /2/notes/evaluate</c> - Evaluate Community Notes. Requires OAuth 2.0
    /// <c>tweet.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<EvaluateNoteResponse>> EvaluateAsync(EvaluateNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PostId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.NoteText);

        return _executor.SendAsync<EvaluateNoteResponse>(
            HttpMethod.Post,
            "2/notes/evaluate",
            new EvaluateNoteBody { PostId = request.PostId, NoteText = request.NoteText },
            queryParameters: null,
            cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /2/notes/{id}</c> - Delete a Community Note. Requires OAuth 2.0
    /// <c>tweet.write</c> or OAuth 1.0a.
    /// </summary>
    public Task<XResponse<DeleteNoteResponse>> DeleteAsync(DeleteNoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Id);

        return _executor.SendAsync<DeleteNoteResponse>(
            HttpMethod.Delete,
            $"2/notes/{Uri.EscapeDataString(request.Id)}",
            cancellationToken);
    }

    /// <summary><c>GET /2/notes/search/notes_written</c> - one page. Requires OAuth 2.0
    /// <c>tweet.read</c> or OAuth 1.0a.</summary>
    public Task<XResponse<SearchNotesWrittenResponse>> SearchNotesWrittenPageAsync(SearchNotesWrittenRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return FetchNotesWrittenPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/notes/search/notes_written</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SearchNotesWrittenResponse>> SearchNotesWrittenPagesAsync(SearchNotesWrittenRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumeratePagesAsync<SearchNotesWrittenResponse>(
            (token, ct) => FetchNotesWrittenPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/notes/search/notes_written</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Note> SearchNotesWrittenAsync(SearchNotesWrittenRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumerateItemsAsync<SearchNotesWrittenResponse, Note>(
            (token, ct) => FetchNotesWrittenPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/notes/search/posts_eligible_for_notes</c> - one page. Requires OAuth
    /// 2.0 <c>tweet.read</c> or OAuth 1.0a.</summary>
    public Task<XResponse<SearchEligiblePostsResponse>> SearchEligiblePostsPageAsync(SearchEligiblePostsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return FetchEligiblePostsPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/notes/search/posts_eligible_for_notes</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<SearchEligiblePostsResponse>> SearchEligiblePostsPagesAsync(SearchEligiblePostsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumeratePagesAsync<SearchEligiblePostsResponse>(
            (token, ct) => FetchEligiblePostsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/notes/search/posts_eligible_for_notes</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<Post> SearchEligiblePostsAsync(SearchEligiblePostsRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumerateItemsAsync<SearchEligiblePostsResponse, Post>(
            (token, ct) => FetchEligiblePostsPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    private Task<XResponse<SearchNotesWrittenResponse>> FetchNotesWrittenPageAsync(SearchNotesWrittenRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("test_mode", request.TestMode ? "true" : "false"),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("note.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<SearchNotesWrittenResponse>(HttpMethod.Get, "2/notes/search/notes_written", query, cancellationToken);
    }

    private Task<XResponse<SearchEligiblePostsResponse>> FetchEligiblePostsPageAsync(SearchEligiblePostsRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var fields = request.Fields;
        var query = new List<(string Name, string? Value)>
        {
            ("test_mode", request.TestMode ? "true" : "false"),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
            ("post_selection", request.PostSelection),
            ("post.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PostFields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(fields?.Expansions, e => e.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(fields?.UserFields, f => f.ToApiValue())),
            ("media.fields", QueryStringBuilder.JoinCommaSeparated(fields?.MediaFields, f => f.ToApiValue())),
            ("poll.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PollFields, f => f.ToApiValue())),
            ("place.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PlaceFields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<SearchEligiblePostsResponse>(HttpMethod.Get, "2/notes/search/posts_eligible_for_notes", query, cancellationToken);
    }
}

using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Pagination;
using XApiSharp.Transport;

namespace XApiSharp.Streaming;

/// <summary>
/// Typed methods for the Stream family (18 operations per the registry, spec section 16): 15
/// NDJSON event streams plus the 3 filtered-stream rule-management operations. Every streaming
/// method returns a lazy <see cref="IAsyncEnumerable{T}"/> built on the shared
/// <see cref="XEventStream"/> engine - nothing connects until the caller starts iterating.
/// </summary>
public sealed class StreamingClient
{
    private readonly RequestExecutor _executor;
    private readonly TimeProvider _timeProvider;

    internal StreamingClient(RequestExecutor executor, TimeProvider timeProvider)
    {
        _executor = executor;
        _timeProvider = timeProvider;
    }

    /// <summary><c>GET /2/tweets/search/stream</c> - Stream filtered Posts, matching whatever
    /// rules are currently active (see <see cref="GetRulesAsync"/>/<see cref="UpdateRulesAsync"/>).
    /// Requires app-only bearer.</summary>
    public IAsyncEnumerable<StreamPostsResponse> StreamPostsAsync(FilteredPostStreamRequest request, XStreamOptions<StreamPostsResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("backfill_minutes", request.BackfillMinutes?.ToString(CultureInfo.InvariantCulture)),
            ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
        };
        AddPostFieldQuery(query, request.Fields);

        return Connect<StreamPostsResponse>("2/tweets/search/stream", query, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/sample/stream</c> - Stream a small sample of all Posts. Requires
    /// app-only bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsSampleAsync(PostSampleStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("backfill_minutes", request.BackfillMinutes?.ToString(CultureInfo.InvariantCulture)),
        };
        AddPostFieldQuery(query, request.Fields);

        return Connect<StreamPostResponse>("2/tweets/sample/stream", query, options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/sample10/stream</c> - Stream a 10% sample of all Posts.
    /// <paramref name="request"/>.Partition is 1-2, per the registry. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsSample10Async(PostVolumeStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamPostResponse>("2/tweets/sample10/stream", BuildPostVolumeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/firehose/stream</c> - Stream all Posts. <paramref name="request"/>.Partition
    /// is 1-20, per the registry. Requires app-only bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsFirehoseAsync(PostVolumeStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamPostResponse>("2/tweets/firehose/stream", BuildPostVolumeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/firehose/stream/lang/en</c> - Stream all English Posts.
    /// <paramref name="request"/>.Partition is 1-8, per the registry. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsFirehoseEnAsync(PostVolumeStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamPostResponse>("2/tweets/firehose/stream/lang/en", BuildPostVolumeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/firehose/stream/lang/ja</c> - Stream all Japanese Posts.
    /// <paramref name="request"/>.Partition is 1-2, per the registry. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsFirehoseJaAsync(PostVolumeStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamPostResponse>("2/tweets/firehose/stream/lang/ja", BuildPostVolumeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/firehose/stream/lang/ko</c> - Stream all Korean Posts.
    /// <paramref name="request"/>.Partition is 1-2, per the registry. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsFirehoseKoAsync(PostVolumeStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamPostResponse>("2/tweets/firehose/stream/lang/ko", BuildPostVolumeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/firehose/stream/lang/pt</c> - Stream all Portuguese Posts.
    /// <paramref name="request"/>.Partition is 1-2, per the registry. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamPostResponse> StreamPostsFirehosePtAsync(PostVolumeStreamRequest request, XStreamOptions<StreamPostResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamPostResponse>("2/tweets/firehose/stream/lang/pt", BuildPostVolumeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/compliance/stream</c> - Stream Posts compliance data.
    /// <paramref name="request"/>.Partition is required (1-4), per the registry. Requires
    /// app-only bearer.</summary>
    public IAsyncEnumerable<StreamComplianceEvent> StreamPostsComplianceAsync(ComplianceStreamRequest request, XStreamOptions<StreamComplianceEvent>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Partition is null)
        {
            throw new ArgumentException("Partition is required for the Posts compliance stream.", nameof(request));
        }

        return Connect<StreamComplianceEvent>("2/tweets/compliance/stream", BuildComplianceQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/label/stream</c> - Stream Post labels. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamComplianceEvent> StreamPostLabelsAsync(ComplianceStreamRequest request, XStreamOptions<StreamComplianceEvent>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamComplianceEvent>("2/tweets/label/stream", BuildComplianceQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/users/compliance/stream</c> - Stream Users compliance data.
    /// <paramref name="request"/>.Partition is required (1-4), per the registry. Requires
    /// app-only bearer.</summary>
    public IAsyncEnumerable<StreamComplianceEvent> StreamUsersComplianceAsync(ComplianceStreamRequest request, XStreamOptions<StreamComplianceEvent>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Partition is null)
        {
            throw new ArgumentException("Partition is required for the Users compliance stream.", nameof(request));
        }

        return Connect<StreamComplianceEvent>("2/users/compliance/stream", BuildComplianceQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/likes/compliance/stream</c> - Stream Likes compliance data. Requires
    /// app-only bearer.</summary>
    public IAsyncEnumerable<StreamComplianceEvent> StreamLikesComplianceAsync(ComplianceStreamRequest request, XStreamOptions<StreamComplianceEvent>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamComplianceEvent>("2/likes/compliance/stream", BuildComplianceQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/likes/firehose/stream</c> - Stream all Likes. <paramref name="request"/>.Partition
    /// is 1-20, per the registry. Requires app-only bearer.</summary>
    public IAsyncEnumerable<StreamLikeResponse> StreamLikesFirehoseAsync(LikeStreamRequest request, XStreamOptions<StreamLikeResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamLikeResponse>("2/likes/firehose/stream", BuildLikeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/likes/sample10/stream</c> - Stream a 10% sample of all Likes.
    /// <paramref name="request"/>.Partition is 1-2, per the registry. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<StreamLikeResponse> StreamLikesSample10Async(LikeStreamRequest request, XStreamOptions<StreamLikeResponse>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<StreamLikeResponse>("2/likes/sample10/stream", BuildLikeQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/activity/stream</c> - Stream activity events (follows, likes, Post
    /// creates/deletes, news, profile updates) for an XAA subscription. Requires app-only
    /// bearer.</summary>
    public IAsyncEnumerable<ActivityStreamEvent> StreamActivityAsync(ComplianceStreamRequest request, XStreamOptions<ActivityStreamEvent>? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Connect<ActivityStreamEvent>("2/activity/stream", BuildComplianceQuery(request), options, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/stream/rules</c> - one page. Requires app-only
    /// bearer.</summary>
    public Task<XResponse<GetStreamRulesResponse>> GetRulesPageAsync(GetStreamRulesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return FetchRulesPageAsync(request, request.PaginationToken, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/stream/rules</c> - lazy page-by-page traversal.</summary>
    public IAsyncEnumerable<XResponse<GetStreamRulesResponse>> GetRulesPagesAsync(GetStreamRulesRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumeratePagesAsync<GetStreamRulesResponse>(
            (token, ct) => FetchRulesPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            options,
            cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/stream/rules</c> - lazy item traversal.</summary>
    public IAsyncEnumerable<StreamRule> GetRulesAsync(GetStreamRulesRequest request, XPaginationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return XPaginator.EnumerateItemsAsync<GetStreamRulesResponse, StreamRule>(
            (token, ct) => FetchRulesPageAsync(request, token ?? request.PaginationToken, ct),
            body => body?.Meta?.NextToken,
            body => body.Data ?? [],
            options,
            cancellationToken);
    }

    /// <summary><c>POST /2/tweets/search/stream/rules</c> - Update stream rules (add/delete).
    /// Requires app-only bearer.</summary>
    public Task<XResponse<UpdateStreamRulesResponse>> UpdateRulesAsync(UpdateStreamRulesRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = new UpdateStreamRulesBody
        {
            Add = request.Add?.Select(a => new UpdateStreamRulesAddBody { Value = a.Value, Tag = a.Tag }).ToList(),
            Delete = request.DeleteIds is null && request.DeleteValues is null
                ? null
                : new UpdateStreamRulesDeleteBody { Ids = request.DeleteIds, Values = request.DeleteValues },
        };

        var query = new List<(string Name, string? Value)>
        {
            ("dry_run", request.DryRun?.ToString().ToLowerInvariant()),
            ("delete_all", request.DeleteAll?.ToString().ToLowerInvariant()),
        };

        return _executor.SendAsync<UpdateStreamRulesResponse>(HttpMethod.Post, "2/tweets/search/stream/rules", body, query, cancellationToken);
    }

    /// <summary><c>GET /2/tweets/search/stream/rules/counts</c> - Get Rule Counts. Requires
    /// app-only bearer.</summary>
    public Task<XResponse<GetStreamRuleCountsResponse>> GetRuleCountsAsync(CancellationToken cancellationToken = default) =>
        _executor.SendAsync<GetStreamRuleCountsResponse>(HttpMethod.Get, "2/tweets/search/stream/rules/counts", cancellationToken);

    private Task<XResponse<GetStreamRulesResponse>> FetchRulesPageAsync(GetStreamRulesRequest request, string? paginationToken, CancellationToken cancellationToken)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", paginationToken),
        };

        return _executor.SendAsync<GetStreamRulesResponse>(HttpMethod.Get, "2/tweets/search/stream/rules", query, cancellationToken);
    }

    private IAsyncEnumerable<TBody> Connect<TBody>(string path, IReadOnlyList<(string Name, string? Value)> query, XStreamOptions<TBody>? options, CancellationToken cancellationToken) =>
        XEventStream.EnumerateAsync(
            ct => _executor.OpenStreamAsync(HttpMethod.Get, path, query, ct),
            options,
            _timeProvider,
            cancellationToken);

    private static List<(string Name, string? Value)> BuildPostVolumeQuery(PostVolumeStreamRequest request)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("backfill_minutes", request.BackfillMinutes?.ToString(CultureInfo.InvariantCulture)),
            ("partition", request.Partition.ToString(CultureInfo.InvariantCulture)),
            ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
        };
        AddPostFieldQuery(query, request.Fields);
        return query;
    }

    private static List<(string Name, string? Value)> BuildComplianceQuery(ComplianceStreamRequest request) =>
    [
        ("backfill_minutes", request.BackfillMinutes?.ToString(CultureInfo.InvariantCulture)),
        ("partition", request.Partition?.ToString(CultureInfo.InvariantCulture)),
        ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
        ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
    ];

    private static List<(string Name, string? Value)> BuildLikeQuery(LikeStreamRequest request)
    {
        var query = new List<(string Name, string? Value)>
        {
            ("backfill_minutes", request.BackfillMinutes?.ToString(CultureInfo.InvariantCulture)),
            ("partition", request.Partition.ToString(CultureInfo.InvariantCulture)),
            ("start_time", request.StartTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("end_time", request.EndTime?.ToString("O", CultureInfo.InvariantCulture)),
            ("like_with_tweet_author.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields?.LikeFields, f => f.ToApiValue())),
            ("expansions", QueryStringBuilder.JoinCommaSeparated(request.Fields?.Expansions, f => f.ToApiValue())),
            ("media.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields?.MediaFields, f => f.ToApiValue())),
            ("user.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields?.UserFields, f => f.ToApiValue())),
            ("tweet.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields?.TweetFields, f => f.ToApiValue())),
        };
        return query;
    }

    private static void AddPostFieldQuery(List<(string Name, string? Value)> query, XStreamPostFieldSelection? fields)
    {
        query.Add(("tweet.fields", QueryStringBuilder.JoinCommaSeparated(fields?.TweetFields, f => f.ToApiValue())));
        query.Add(("expansions", QueryStringBuilder.JoinCommaSeparated(fields?.Expansions, f => f.ToApiValue())));
        query.Add(("media.fields", QueryStringBuilder.JoinCommaSeparated(fields?.MediaFields, f => f.ToApiValue())));
        query.Add(("poll.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PollFields, f => f.ToApiValue())));
        query.Add(("user.fields", QueryStringBuilder.JoinCommaSeparated(fields?.UserFields, f => f.ToApiValue())));
        query.Add(("place.fields", QueryStringBuilder.JoinCommaSeparated(fields?.PlaceFields, f => f.ToApiValue())));
    }
}

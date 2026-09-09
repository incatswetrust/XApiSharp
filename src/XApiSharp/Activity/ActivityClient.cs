using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Activity;

/// <summary>
/// Typed methods for the Activity family (5 operations per the registry) - subscription
/// management for the activity feed <see cref="Streaming.StreamingClient.StreamActivityAsync"/>
/// reads (or, when a subscription sets <c>webhook_id</c>, delivers to a webhook instead of/as well
/// as the stream). Distinct from the older, per-user <see cref="Webhooks.WebhooksClient"/>
/// <c>*AccountActivitySubscription*</c> methods.
/// </summary>
public sealed class ActivityClient
{
    private readonly RequestExecutor _executor;

    internal ActivityClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// <c>GET /2/activity/subscriptions</c> - Get X activity subscriptions. Requires OAuth 2.0
    /// (<c>tweet.read</c> + <c>like.read</c>), OAuth 1.0a, or app-only bearer. See
    /// <see cref="GetActivitySubscriptionsRequest"/> - single-page only, the response has no
    /// continuation token in the current contract.
    /// </summary>
    public Task<XResponse<GetActivitySubscriptionsResponse>> GetSubscriptionsAsync(GetActivitySubscriptionsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("max_results", request.MaxResults?.ToString(CultureInfo.InvariantCulture)),
            ("pagination_token", request.PaginationToken),
        };

        return _executor.SendAsync<GetActivitySubscriptionsResponse>(HttpMethod.Get, "2/activity/subscriptions", query, cancellationToken);
    }

    /// <summary><c>POST /2/activity/subscriptions</c> - Create X activity subscription. Requires
    /// OAuth 2.0, OAuth 1.0a, or app-only bearer.</summary>
    public Task<XResponse<CreateActivitySubscriptionResponse>> CreateSubscriptionAsync(CreateActivitySubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Filter);

        var body = new CreateActivitySubscriptionBody
        {
            EventType = request.EventType.ToApiValue(),
            Filter = ActivitySubscriptionFilterMapper.ToBody(request.Filter)!,
            Tag = request.Tag,
            WebhookId = request.WebhookId,
        };

        return _executor.SendAsync<CreateActivitySubscriptionResponse>(HttpMethod.Post, "2/activity/subscriptions", body, queryParameters: null, cancellationToken);
    }

    /// <summary><c>DELETE /2/activity/subscriptions</c> - Delete X activity subscriptions by IDs
    /// (bulk, up to 100). Requires app-only bearer. A per-ID partial-success shape - check each
    /// result/error entry rather than assuming an HTTP 200 means every ID was deleted.</summary>
    public Task<XResponse<DeleteActivitySubscriptionsByIdsResponse>> DeleteSubscriptionsByIdsAsync(DeleteActivitySubscriptionsByIdsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Ids.Count == 0)
        {
            throw new ArgumentException("At least one subscription ID is required.", nameof(request));
        }

        var query = new List<(string Name, string? Value)>
        {
            ("ids", QueryStringBuilder.JoinCommaSeparated(request.Ids)),
        };

        return _executor.SendAsync<DeleteActivitySubscriptionsByIdsResponse>(HttpMethod.Delete, "2/activity/subscriptions", query, cancellationToken);
    }

    /// <summary><c>DELETE /2/activity/subscriptions/{subscription_id}</c> - Delete one X activity
    /// subscription. Requires OAuth 2.0, OAuth 1.0a, or app-only bearer.</summary>
    public Task<XResponse<DeleteActivitySubscriptionResponse>> DeleteSubscriptionAsync(DeleteActivitySubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SubscriptionId);

        return _executor.SendAsync<DeleteActivitySubscriptionResponse>(
            HttpMethod.Delete,
            $"2/activity/subscriptions/{Uri.EscapeDataString(request.SubscriptionId)}",
            cancellationToken);
    }

    /// <summary><c>PUT /2/activity/subscriptions/{subscription_id}</c> - Update X activity
    /// subscription. Requires OAuth 2.0, OAuth 1.0a, or app-only bearer.</summary>
    public Task<XResponse<UpdateActivitySubscriptionResponse>> UpdateSubscriptionAsync(UpdateActivitySubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SubscriptionId);

        var body = new UpdateActivitySubscriptionBody { Tag = request.Tag, WebhookId = request.WebhookId };

        return _executor.SendAsync<UpdateActivitySubscriptionResponse>(
            HttpMethod.Put,
            $"2/activity/subscriptions/{Uri.EscapeDataString(request.SubscriptionId)}",
            body,
            queryParameters: null,
            cancellationToken);
    }
}

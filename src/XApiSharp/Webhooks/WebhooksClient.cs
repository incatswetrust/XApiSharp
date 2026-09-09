using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Webhooks;

/// <summary>
/// Typed methods for the Webhooks family (8 operations per the registry, spec section 17.1):
/// webhook create/read/validate/delete, replay jobs, and filtered-stream link management - plus 5
/// Account Activity subscription operations (registry group "Account Activity", not tied to any
/// roadmap stage's subtask list until this addition; see issue #18) that share this client because
/// they take the same <c>webhook_id</c> <see cref="CreateWebhooksResponseData.Id"/> returns. The
/// CRC challenge/signature-verification helpers your app's own receiving endpoint needs
/// (<see cref="XWebhookChallengeResponder"/>/<see cref="XWebhookSignatureVerifier"/>) live
/// alongside this client but don't depend on it - they run on the inbound side, not as outgoing
/// SDK calls. This SDK does not include a hosted webhook server (spec 17.1) - see the ASP.NET Core
/// sample for wiring these helpers into your own endpoint.
/// </summary>
public sealed class WebhooksClient
{
    private readonly RequestExecutor _executor;

    internal WebhooksClient(RequestExecutor executor)
    {
        _executor = executor;
    }

    /// <summary><c>GET /2/tweets/search/webhooks</c> - Get stream links (which webhooks are
    /// linked to the filtered Post stream). Requires app-only bearer.</summary>
    public Task<XResponse<GetWebhooksStreamLinksResponse>> GetStreamLinksAsync(GetWebhooksStreamLinksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _executor.SendAsync<GetWebhooksStreamLinksResponse>(HttpMethod.Get, "2/tweets/search/webhooks", cancellationToken);
    }

    /// <summary><c>DELETE /2/tweets/search/webhooks/{webhook_id}</c> - Delete stream link.
    /// Requires app-only bearer.</summary>
    public Task<XResponse<DeleteWebhooksStreamLinkResponse>> DeleteStreamLinkAsync(DeleteWebhooksStreamLinkRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<DeleteWebhooksStreamLinkResponse>(
            HttpMethod.Delete,
            $"2/tweets/search/webhooks/{Uri.EscapeDataString(request.WebhookId)}",
            cancellationToken);
    }

    /// <summary><c>POST /2/tweets/search/webhooks/{webhook_id}</c> - Create stream link (links
    /// the filtered Post stream to this webhook). Requires app-only bearer.</summary>
    public Task<XResponse<CreateWebhooksStreamLinkResponse>> CreateStreamLinkAsync(CreateWebhooksStreamLinkRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<CreateWebhooksStreamLinkResponse>(
            HttpMethod.Post,
            $"2/tweets/search/webhooks/{Uri.EscapeDataString(request.WebhookId)}",
            cancellationToken);
    }

    /// <summary><c>GET /2/webhooks</c> - Get webhook configurations. Requires OAuth 2.0, OAuth
    /// 1.0a, or app-only bearer.</summary>
    public Task<XResponse<GetWebhooksResponse>> GetAllAsync(GetWebhooksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new List<(string Name, string? Value)>
        {
            ("webhook_config.fields", QueryStringBuilder.JoinCommaSeparated(request.Fields, f => f.ToApiValue())),
        };

        return _executor.SendAsync<GetWebhooksResponse>(HttpMethod.Get, "2/webhooks", query, cancellationToken);
    }

    /// <summary><c>POST /2/webhooks</c> - Create webhook. Requires OAuth 2.0, OAuth 1.0a, or
    /// app-only bearer.</summary>
    public Task<XResponse<CreateWebhooksResponse>> CreateAsync(CreateWebhooksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Url);

        var body = new CreateWebhooksBody { Url = request.Url };

        return _executor.SendAsync<CreateWebhooksResponse>(HttpMethod.Post, "2/webhooks", body, queryParameters: null, cancellationToken);
    }

    /// <summary><c>POST /2/webhooks/replay</c> - Create a replay job to redeliver events for a
    /// webhook over a time range (spec section 17.2: persist <c>JobId</c>, don't resubmit on an
    /// unknown outcome). Requires app-only bearer.</summary>
    public Task<XResponse<CreateWebhookReplayJobResponse>> CreateReplayJobAsync(CreateWebhookReplayJobRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        var body = new CreateWebhookReplayJobBody
        {
            WebhookId = request.WebhookId,
            FromDate = request.FromDate.UtcDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture),
            ToDate = request.ToDate.UtcDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture),
        };

        return _executor.SendAsync<CreateWebhookReplayJobResponse>(HttpMethod.Post, "2/webhooks/replay", body, queryParameters: null, cancellationToken);
    }

    /// <summary><c>DELETE /2/webhooks/{webhook_id}</c> - Delete webhook. Requires OAuth 2.0,
    /// OAuth 1.0a, or app-only bearer.</summary>
    public Task<XResponse<DeleteWebhooksResponse>> DeleteAsync(DeleteWebhooksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<DeleteWebhooksResponse>(HttpMethod.Delete, $"2/webhooks/{Uri.EscapeDataString(request.WebhookId)}", cancellationToken);
    }

    /// <summary><c>PUT /2/webhooks/{webhook_id}</c> - Validate webhook (re-triggers the CRC
    /// challenge against the registered URL; see <see cref="XWebhookChallengeResponder"/> for
    /// answering it). Requires OAuth 2.0, OAuth 1.0a, or app-only bearer.</summary>
    public Task<XResponse<ValidateWebhooksResponse>> ValidateAsync(ValidateWebhooksRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<ValidateWebhooksResponse>(HttpMethod.Put, $"2/webhooks/{Uri.EscapeDataString(request.WebhookId)}", cancellationToken);
    }

    /// <summary><c>GET /2/account_activity/webhooks/{webhook_id}/subscriptions/all</c> - Validate
    /// Account Activity Subscription (checks whether the authenticated user has one). Requires
    /// OAuth 2.0 (<c>users.read</c> + <c>tweet.read</c> + <c>dm.write</c> + <c>dm.read</c>) or
    /// OAuth 1.0a.</summary>
    public Task<XResponse<ValidateAccountActivitySubscriptionResponse>> ValidateAccountActivitySubscriptionAsync(ValidateAccountActivitySubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<ValidateAccountActivitySubscriptionResponse>(
            HttpMethod.Get,
            $"2/account_activity/webhooks/{Uri.EscapeDataString(request.WebhookId)}/subscriptions/all",
            cancellationToken);
    }

    /// <summary><c>POST /2/account_activity/webhooks/{webhook_id}/subscriptions/all</c> - Create
    /// subscription (subscribes the authenticated user to Account Activity events on this
    /// webhook). Requires OAuth 2.0 (<c>dm.write</c> + <c>users.read</c> + <c>tweet.read</c> +
    /// <c>dm.read</c>) or OAuth 1.0a.</summary>
    public Task<XResponse<CreateAccountActivitySubscriptionResponse>> CreateAccountActivitySubscriptionAsync(CreateAccountActivitySubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<CreateAccountActivitySubscriptionResponse>(
            HttpMethod.Post,
            $"2/account_activity/webhooks/{Uri.EscapeDataString(request.WebhookId)}/subscriptions/all",
            cancellationToken);
    }

    /// <summary><c>GET /2/account_activity/webhooks/{webhook_id}/subscriptions/all/list</c> - Get
    /// Account Activity Subscriptions (every user subscribed to this webhook). Requires app-only
    /// bearer.</summary>
    public Task<XResponse<GetAccountActivitySubscriptionsResponse>> GetAccountActivitySubscriptionsAsync(GetAccountActivitySubscriptionsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);

        return _executor.SendAsync<GetAccountActivitySubscriptionsResponse>(
            HttpMethod.Get,
            $"2/account_activity/webhooks/{Uri.EscapeDataString(request.WebhookId)}/subscriptions/all/list",
            cancellationToken);
    }

    /// <summary><c>DELETE /2/account_activity/webhooks/{webhook_id}/subscriptions/{user_id}/all</c>
    /// - Delete subscription (unsubscribes a user). Requires app-only bearer.</summary>
    public Task<XResponse<DeleteAccountActivitySubscriptionResponse>> DeleteAccountActivitySubscriptionAsync(DeleteAccountActivitySubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WebhookId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UserId);

        return _executor.SendAsync<DeleteAccountActivitySubscriptionResponse>(
            HttpMethod.Delete,
            $"2/account_activity/webhooks/{Uri.EscapeDataString(request.WebhookId)}/subscriptions/{Uri.EscapeDataString(request.UserId)}/all",
            cancellationToken);
    }

    /// <summary><c>GET /2/account_activity/subscriptions/count</c> - Get Account Activity
    /// Subscription Count. Requires app-only bearer.</summary>
    public Task<XResponse<GetAccountActivitySubscriptionCountResponse>> GetAccountActivitySubscriptionCountAsync(GetAccountActivitySubscriptionCountRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _executor.SendAsync<GetAccountActivitySubscriptionCountResponse>(HttpMethod.Get, "2/account_activity/subscriptions/count", cancellationToken);
    }
}

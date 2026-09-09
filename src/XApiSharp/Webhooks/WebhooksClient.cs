using System.Globalization;
using XApiSharp.Common;
using XApiSharp.Transport;

namespace XApiSharp.Webhooks;

/// <summary>
/// Typed methods for the Webhooks family (8 operations per the registry, spec section 17.1):
/// webhook create/read/validate/delete, replay jobs, and filtered-stream link management. The
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
}

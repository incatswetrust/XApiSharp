using XApiSharp.Account;
using XApiSharp.Articles;
using XApiSharp.Authentication;
using XApiSharp.Bots;
using XApiSharp.Broadcasts;
using XApiSharp.Communities;
using XApiSharp.CommunityNotes;
using XApiSharp.Compliance;
using XApiSharp.Connections;
using XApiSharp.DirectMessages;
using XApiSharp.General;
using XApiSharp.Lists;
using XApiSharp.Media;
using XApiSharp.News;
using XApiSharp.Posts;
using XApiSharp.Spaces;
using XApiSharp.Transport;
using XApiSharp.Trends;
using XApiSharp.Usage;
using XApiSharp.Users;

namespace XApiSharp;

/// <summary>
/// Entry point for the SDK. Groups endpoint clients by entity (spec section 8.1). The caller owns
/// the <see cref="HttpClient"/>'s lifetime (HTTP-01) - this type never disposes it.
/// </summary>
public sealed class XApiClient
{
    /// <param name="httpClient">Externally owned - never disposed by this type (HTTP-01).</param>
    /// <param name="authenticationProvider">Prepares auth for every outgoing request.</param>
    /// <param name="options">Defaults to <c>new XClientOptions()</c> when omitted.</param>
    /// <param name="timeProvider">Defaults to <see cref="TimeProvider.System"/>. Inject a fake
    /// provider in tests to control <see cref="XClientOptions.OperationTimeout"/>/
    /// <see cref="XClientOptions.AttemptTimeout"/> deterministically, without real sleeps.</param>
    /// <param name="retryJitterSource">Defaults to <see cref="Random.Shared"/>. Inject a seeded
    /// <see cref="Random"/> in tests for deterministic retry-delay assertions (spec: "jitter
    /// через контролируемый random").</param>
    public XApiClient(HttpClient httpClient, IXAuthenticationProvider authenticationProvider, XClientOptions? options = null, TimeProvider? timeProvider = null, Random? retryJitterSource = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(authenticationProvider);

        var resolvedOptions = options ?? new XClientOptions();
        var resolvedTimeProvider = timeProvider ?? TimeProvider.System;
        var resolvedJitterSource = retryJitterSource ?? Random.Shared;
        var executor = new RequestExecutor(httpClient, authenticationProvider, resolvedOptions, resolvedTimeProvider, resolvedJitterSource);

        Users = new UsersClient(executor);
        Posts = new PostsClient(executor);
        Lists = new ListsClient(executor);
        DirectMessages = new DirectMessagesClient(executor);
        Spaces = new SpacesClient(executor);
        CommunityNotes = new CommunityNotesClient(executor);
        Communities = new CommunitiesClient(executor);
        Articles = new ArticlesClient(executor);
        Trends = new TrendsClient(executor);
        News = new NewsClient(executor);
        Usage = new UsageClient(executor);
        Account = new AccountClient(executor);
        General = new GeneralClient(executor);
        Compliance = new ComplianceClient(executor);
        Connections = new ConnectionsClient(executor);
        Bots = new BotsClient(executor);
        Broadcasts = new BroadcastsClient(executor);
        Media = new MediaClient(executor);
    }

    public UsersClient Users { get; }

    public PostsClient Posts { get; }

    public ListsClient Lists { get; }

    public DirectMessagesClient DirectMessages { get; }

    public SpacesClient Spaces { get; }

    public CommunityNotesClient CommunityNotes { get; }

    public CommunitiesClient Communities { get; }

    public ArticlesClient Articles { get; }

    public TrendsClient Trends { get; }

    public NewsClient News { get; }

    public UsageClient Usage { get; }

    public AccountClient Account { get; }

    public GeneralClient General { get; }

    public ComplianceClient Compliance { get; }

    public ConnectionsClient Connections { get; }

    public BotsClient Bots { get; }

    public BroadcastsClient Broadcasts { get; }

    public MediaClient Media { get; }
}

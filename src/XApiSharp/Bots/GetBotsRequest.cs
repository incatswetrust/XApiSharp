using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;
using XApiSharp.Users;

namespace XApiSharp.Bots;

/// <summary>Request for <c>GET /2/bots</c>. Not paginated - the registry declares only
/// <c>max_bots</c>/<c>result_count</c> hints in <c>meta</c>, no continuation token (bot accounts
/// are capped at a small fixed count per app, like pinned lists).</summary>
public sealed class GetBotsRequest
{
    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    /// <summary>Only <c>affiliation</c>/<c>most_recent_post_id</c>/<c>pinned_post_id</c> are
    /// meaningful here, per the registry.</summary>
    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

/// <summary>Modeled from the "GetBotsResponse" schema - a bot's account is a <see cref="User"/>,
/// per the registry.</summary>
public sealed class GetBotsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<User>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public GetBotsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class GetBotsMeta
{
    [JsonPropertyName("max_bots")]
    public int? MaxBots { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Connections;

/// <summary>Request for <c>GET /2/connections</c>.</summary>
public sealed class GetConnectionHistoryRequest
{
    public XConnectionStatus? Status { get; init; }

    public IReadOnlyCollection<XStreamEndpoint>? Endpoints { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XConnectionField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetConnectionHistoryResponse" schema.</summary>
public sealed class GetConnectionHistoryResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Connection>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public ConnectionHistoryMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

/// <summary>Not <see cref="XPageMeta"/> - no <c>previous_token</c> in this operation's schema.</summary>
public sealed class ConnectionHistoryMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

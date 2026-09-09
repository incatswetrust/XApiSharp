using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>GET /2/chat/conversations</c>.</summary>
public sealed class GetChatConversationsRequest
{
    /// <summary>1-100, default 10, per the registry.</summary>
    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>meta.next_token</c>, never
    /// constructed or decoded by the caller.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XChatConversationField>? Fields { get; init; }

    public IReadOnlyCollection<XChatExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }
}

/// <summary>Modeled from the "GetChatConversationsResponse" schema.</summary>
public sealed class GetChatConversationsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ChatConversation>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public GetChatConversationsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class GetChatConversationsMeta
{
    [JsonPropertyName("has_message_requests")]
    public bool? HasMessageRequests { get; init; }

    [JsonPropertyName("has_more")]
    public bool? HasMore { get; init; }

    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

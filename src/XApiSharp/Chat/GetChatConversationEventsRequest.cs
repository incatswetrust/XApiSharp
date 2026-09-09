using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>GET /2/chat/conversations/{id}/events</c>.</summary>
public sealed class GetChatConversationEventsRequest
{
    public required string ConversationId { get; init; }

    /// <summary>1-100, default 10, per the registry.</summary>
    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>meta.next_token</c>, never
    /// constructed or decoded by the caller.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XChatMessageEventField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetChatConversationEventsResponse" schema.</summary>
public sealed class GetChatConversationEventsResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<ChatMessageEvent>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public GetChatConversationEventsMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class GetChatConversationEventsMeta
{
    /// <summary>Opaque per-event key-change markers, per the registry - carried through
    /// unmodified, never interpreted by the SDK (spec 3.2).</summary>
    [JsonPropertyName("conversation_key_events")]
    public IReadOnlyList<string>? ConversationKeyEvents { get; init; }

    [JsonPropertyName("has_more")]
    public bool? HasMore { get; init; }

    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

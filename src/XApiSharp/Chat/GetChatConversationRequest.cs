using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Chat;

/// <summary>Request for <c>GET /2/chat/conversations/{id}</c>.</summary>
public sealed class GetChatConversationRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XChatConversationField>? Fields { get; init; }

    public IReadOnlyCollection<XChatExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XUserField>? UserFields { get; init; }
}

/// <summary>Modeled from the "GetChatConversationResponse" schema.</summary>
public sealed class GetChatConversationResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public ChatConversation? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>DELETE /2/broadcasts/{id}/chat/{message_id}</c>.</summary>
public sealed class DeleteBroadcastChatMessageRequest
{
    public required string Id { get; init; }

    public required string MessageId { get; init; }
}

/// <summary>Modeled from the "DeleteBroadcastChatMessageResponse" schema.</summary>
public sealed class DeleteBroadcastChatMessageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteBroadcastChatMessageResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteBroadcastChatMessageResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>POST /2/broadcasts/{id}/chat</c>.</summary>
public sealed class SendBroadcastChatRequest
{
    public required string Id { get; init; }

    /// <summary>1-140 characters, per the registry.</summary>
    public required string Text { get; init; }

    public string? ReplyTo { get; init; }
}

/// <summary>Modeled from the "SendBroadcastChatResponse" schema.</summary>
public sealed class SendBroadcastChatResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public SendBroadcastChatResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class SendBroadcastChatResponseData
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>Server timestamp of the message, in nanoseconds, per the registry.</summary>
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; init; }
}

internal sealed class SendBroadcastChatBody
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("reply_to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReplyTo { get; init; }
}

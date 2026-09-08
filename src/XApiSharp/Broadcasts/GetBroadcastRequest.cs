using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Broadcasts;

/// <summary>Request for <c>GET /2/broadcasts/{id}</c>.</summary>
public sealed class GetBroadcastRequest
{
    public required string Id { get; init; }

    public IReadOnlyCollection<XBroadcastField>? Fields { get; init; }
}

/// <summary>Modeled from the "GetBroadcastResponse" schema.</summary>
public sealed class GetBroadcastResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public Broadcast? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

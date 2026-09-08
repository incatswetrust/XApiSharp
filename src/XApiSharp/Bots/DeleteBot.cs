using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Bots;

/// <summary>Request for <c>DELETE /2/bots/{id}</c>.</summary>
public sealed class DeleteBotRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "DeleteBotResponse" schema.</summary>
public sealed class DeleteBotResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteBotResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteBotResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

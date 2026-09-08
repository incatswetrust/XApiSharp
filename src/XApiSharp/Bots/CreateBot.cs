using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Bots;

/// <summary>Request for <c>POST /2/bots</c>.</summary>
public sealed class CreateBotRequest
{
    /// <summary>5-15 characters, alphanumeric/underscore, per the registry.</summary>
    public required string Handle { get; init; }

    /// <summary>1-50 characters, per the registry.</summary>
    public string? DisplayName { get; init; }

    /// <summary>No documented closed enum of valid scope values in the registry - raw
    /// passthrough strings.</summary>
    public IReadOnlyCollection<string>? Scopes { get; init; }
}

/// <summary>Modeled from the "CreateBotResponse" schema. Returned with HTTP 201.</summary>
public sealed class CreateBotResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public BotToken? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

internal sealed class CreateBotBody
{
    [JsonPropertyName("handle")]
    public required string Handle { get; init; }

    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }

    [JsonPropertyName("scopes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? Scopes { get; init; }
}

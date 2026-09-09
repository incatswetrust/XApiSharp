using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Streaming;

/// <summary>Request for <c>POST /2/tweets/search/stream/rules</c> - add and/or delete rules in
/// one call. At least one of <see cref="Add"/>/<see cref="DeleteIds"/>/<see cref="DeleteValues"/>
/// should be set; the SDK doesn't invent a client-side "must have at least one" check beyond what
/// the schema itself requires (API-10), the server validates an empty request.</summary>
public sealed class UpdateStreamRulesRequest
{
    public IReadOnlyCollection<StreamRuleToAdd>? Add { get; init; }

    /// <summary>Rule ids (Snowflake external ids) to delete.</summary>
    public IReadOnlyCollection<string>? DeleteIds { get; init; }

    /// <summary>Rule filter-expression values to delete.</summary>
    public IReadOnlyCollection<string>? DeleteValues { get; init; }

    /// <summary>Validate without actually applying the change.</summary>
    public bool? DryRun { get; init; }

    /// <summary>Delete every existing rule before applying <see cref="Add"/>.</summary>
    public bool? DeleteAll { get; init; }
}

public sealed class StreamRuleToAdd
{
    public required string Value { get; init; }

    public string? Tag { get; init; }
}

/// <summary>Modeled from the "UpdateRulesResponse" schema.</summary>
public sealed class UpdateStreamRulesResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<StreamRule>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public UpdateStreamRulesMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

public sealed class UpdateStreamRulesMeta
{
    [JsonPropertyName("sent")]
    public string? Sent { get; init; }

    /// <summary>SER-09/SER-12 escape hatch - an untyped summary object per the registry.</summary>
    [JsonPropertyName("summary")]
    public System.Text.Json.JsonElement? Summary { get; init; }
}

internal sealed class UpdateStreamRulesBody
{
    [JsonPropertyName("add")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<UpdateStreamRulesAddBody>? Add { get; init; }

    [JsonPropertyName("delete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UpdateStreamRulesDeleteBody? Delete { get; init; }
}

internal sealed class UpdateStreamRulesAddBody
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; init; }
}

internal sealed class UpdateStreamRulesDeleteBody
{
    [JsonPropertyName("ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? Ids { get; init; }

    [JsonPropertyName("values")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? Values { get; init; }
}

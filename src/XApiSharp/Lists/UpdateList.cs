using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Lists;

/// <summary>Request for <c>PUT /2/lists/{id}</c>. Both fields optional - only the ones set are
/// sent, per the registry (a PATCH-like partial update despite the PUT verb).</summary>
public sealed class UpdateListRequest
{
    public required string Id { get; init; }

    public string? Name { get; init; }

    public bool? Private { get; init; }
}

/// <summary>Modeled from the "UpdateListsResponse" schema.</summary>
public sealed class UpdateListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UpdateListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UpdateListResponseData
{
    [JsonPropertyName("updated")]
    public bool Updated { get; init; }
}

internal sealed class UpdateListBody
{
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    [JsonPropertyName("private")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Private { get; init; }
}

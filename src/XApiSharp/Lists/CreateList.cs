using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.Lists;

/// <summary>Request for <c>POST /2/lists</c>.</summary>
public sealed class CreateListRequest
{
    /// <summary>1-25 characters, per the registry.</summary>
    public required string Name { get; init; }

    public bool? Private { get; init; }
}

/// <summary>Modeled from the "CreateListsResponse" schema. Returned with HTTP 201.</summary>
public sealed class CreateListResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateListResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateListResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

internal sealed class CreateListBody
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("private")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Private { get; init; }
}

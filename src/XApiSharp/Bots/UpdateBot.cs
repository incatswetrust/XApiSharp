using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Bots;

/// <summary>Request for <c>PUT /2/bots/{id}</c>. All fields optional - only the ones set are
/// sent (a PATCH-like partial update despite the PUT verb, same pattern as
/// <see cref="Lists.UpdateListRequest"/>).</summary>
public sealed class UpdateBotRequest
{
    public required string Id { get; init; }

    public string? Handle { get; init; }

    public string? DisplayName { get; init; }

    public XBotDmAccess? DmPermission { get; init; }
}

/// <summary>Modeled from the "UpdateBotResponse" schema.</summary>
public sealed class UpdateBotResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public UpdateBotResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class UpdateBotResponseData
{
    [JsonPropertyName("updated")]
    public bool Updated { get; init; }
}

internal sealed class UpdateBotBody
{
    [JsonPropertyName("handle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Handle { get; init; }

    [JsonPropertyName("display_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }

    [JsonPropertyName("dm_permission")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DmPermission { get; init; }
}

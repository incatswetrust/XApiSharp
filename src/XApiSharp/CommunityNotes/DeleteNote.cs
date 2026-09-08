using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.CommunityNotes;

/// <summary>Request for <c>DELETE /2/notes/{id}</c>.</summary>
public sealed class DeleteNoteRequest
{
    public required string Id { get; init; }
}

/// <summary>Modeled from the "DeleteCommunityNotesResponse" schema.</summary>
public sealed class DeleteNoteResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public DeleteNoteResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class DeleteNoteResponseData
{
    [JsonPropertyName("deleted")]
    public bool Deleted { get; init; }
}

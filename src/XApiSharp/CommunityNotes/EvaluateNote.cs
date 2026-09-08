using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.CommunityNotes;

/// <summary>Request for <c>POST /2/notes/evaluate</c>.</summary>
public sealed class EvaluateNoteRequest
{
    public required string PostId { get; init; }

    public required string NoteText { get; init; }
}

/// <summary>Modeled from the "EvaluateCommunityNotesResponse" schema.</summary>
public sealed class EvaluateNoteResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public EvaluateNoteResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class EvaluateNoteResponseData
{
    [JsonPropertyName("claim_opinion_score")]
    public double? ClaimOpinionScore { get; init; }
}

internal sealed class EvaluateNoteBody
{
    [JsonPropertyName("post_id")]
    public required string PostId { get; init; }

    [JsonPropertyName("note_text")]
    public required string NoteText { get; init; }
}

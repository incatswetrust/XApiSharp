using System.Text.Json.Serialization;
using XApiSharp.Errors;

namespace XApiSharp.CommunityNotes;

/// <summary>Request for <c>POST /2/notes</c>.</summary>
public sealed class CreateNoteRequest
{
    public required string PostId { get; init; }

    /// <summary>Required by the registry - <see langword="true"/> submits the note in test mode
    /// (not shown to real users), which every consumer should default to until explicitly opting
    /// into production submission.</summary>
    public required bool TestMode { get; init; }

    public required CreateNoteInfo Info { get; init; }
}

public sealed class CreateNoteInfo
{
    /// <summary>Must contain a URL, per the registry's pattern constraint.</summary>
    public required string Text { get; init; }

    public required NoteClassification Classification { get; init; }

    public required bool TrustworthySources { get; init; }

    public IReadOnlyCollection<MisleadingTag>? MisleadingTags { get; init; }

    public bool? IsMediaNote { get; init; }
}

public enum NoteClassification
{
    MisinformedOrPotentiallyMisleading,
    NotMisleading,
}

internal static class NoteClassificationExtensions
{
    public static string ToApiValue(this NoteClassification value) => value switch
    {
        NoteClassification.MisinformedOrPotentiallyMisleading => "misinformed_or_potentially_misleading",
        NoteClassification.NotMisleading => "not_misleading",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

public enum MisleadingTag
{
    DisputedClaimAsFact,
    FactualError,
    ManipulatedMedia,
    MisinterpretedSatire,
    MissingImportantContext,
    Other,
    OutdatedInformation,
}

internal static class MisleadingTagExtensions
{
    public static string ToApiValue(this MisleadingTag value) => value switch
    {
        MisleadingTag.DisputedClaimAsFact => "disputed_claim_as_fact",
        MisleadingTag.FactualError => "factual_error",
        MisleadingTag.ManipulatedMedia => "manipulated_media",
        MisleadingTag.MisinterpretedSatire => "misinterpreted_satire",
        MisleadingTag.MissingImportantContext => "missing_important_context",
        MisleadingTag.Other => "other",
        MisleadingTag.OutdatedInformation => "outdated_information",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

/// <summary>Modeled from the "CreateCommunityNotesResponse" schema. Returned with HTTP 201.</summary>
public sealed class CreateNoteResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public CreateNoteResponseData? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is not null && HasErrors;
}

public sealed class CreateNoteResponseData
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
}

internal sealed class CreateNoteBody
{
    [JsonPropertyName("post_id")]
    public required string PostId { get; init; }

    [JsonPropertyName("test_mode")]
    public required bool TestMode { get; init; }

    [JsonPropertyName("info")]
    public required CreateNoteInfoBody Info { get; init; }
}

internal sealed class CreateNoteInfoBody
{
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    [JsonPropertyName("classification")]
    public required string Classification { get; init; }

    [JsonPropertyName("trustworthy_sources")]
    public required bool TrustworthySources { get; init; }

    [JsonPropertyName("misleading_tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? MisleadingTags { get; init; }

    [JsonPropertyName("is_media_note")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsMediaNote { get; init; }
}

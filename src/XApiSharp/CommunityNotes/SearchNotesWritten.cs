using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.CommunityNotes;

/// <summary>Request for <c>GET /2/notes/search/notes_written</c>.</summary>
public sealed class SearchNotesWrittenRequest
{
    /// <summary>Required by the registry.</summary>
    public required bool TestMode { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>Meta.NextToken</c>.</summary>
    public string? PaginationToken { get; init; }

    public IReadOnlyCollection<XNoteField>? Fields { get; init; }
}

/// <summary>Modeled from the "SearchCommunityNotesWrittenResponse" schema.</summary>
public sealed class SearchNotesWrittenResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<Note>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("meta")]
    public SearchNotesMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

/// <summary>Not <see cref="XPageMeta"/> - no <c>previous_token</c> in this operation's schema.</summary>
public sealed class SearchNotesMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

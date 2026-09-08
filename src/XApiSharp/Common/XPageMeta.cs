using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>
/// The <c>meta</c> object every pageable operation in the registry returns (PAGE-01..10 build on
/// reading <see cref="NextToken"/> from this). <see cref="PreviousToken"/> is exposed for
/// completeness but the SDK's pagination helpers only ever walk forward via
/// <see cref="NextToken"/> - X's own docs don't document backward paging as a supported traversal.
/// </summary>
public sealed class XPageMeta
{
    [JsonPropertyName("next_token")]
    public string? NextToken { get; init; }

    [JsonPropertyName("previous_token")]
    public string? PreviousToken { get; init; }

    [JsonPropertyName("result_count")]
    public int? ResultCount { get; init; }
}

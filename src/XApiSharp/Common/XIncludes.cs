using System.Text.Json.Serialization;
using XApiSharp.Users;

namespace XApiSharp.Common;

/// <summary>
/// Modeled from the "Expansions" schema - the shape of an operation's <c>includes</c> object
/// (SER-05), populated according to whichever <see cref="XExpansion"/> values the request asked
/// for. Identical across every operation that supports expansions, so it's one shared type rather
/// than a copy nested inside each response class.
/// </summary>
public sealed class XIncludes
{
    [JsonPropertyName("users")]
    public IReadOnlyList<User>? Users { get; init; }

    [JsonPropertyName("posts")]
    public IReadOnlyList<Post>? Posts { get; init; }

    [JsonPropertyName("media")]
    public IReadOnlyList<Media>? Media { get; init; }

    [JsonPropertyName("places")]
    public IReadOnlyList<Place>? Places { get; init; }

    [JsonPropertyName("polls")]
    public IReadOnlyList<Poll>? Polls { get; init; }

    [JsonPropertyName("topics")]
    public IReadOnlyList<Topic>? Topics { get; init; }
}

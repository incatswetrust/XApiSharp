using System.Text.Json.Serialization;

namespace XApiSharp.Common;

/// <summary>
/// Modeled from the "List" schema. Named <c>XList</c>, not <c>List</c> - the registry's own name
/// collides with <see cref="System.Collections.Generic.List{T}"/>, which every file in this
/// codebase can see via <c>ImplicitUsings</c>; reusing it would make every ordinary
/// <c>List&lt;T&gt;</c> declaration in scope of this type ambiguous.
/// </summary>
public sealed class XList
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; init; }

    [JsonPropertyName("private")]
    public bool? Private { get; init; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonPropertyName("follower_count")]
    public long? FollowerCount { get; init; }

    [JsonPropertyName("member_count")]
    public long? MemberCount { get; init; }
}

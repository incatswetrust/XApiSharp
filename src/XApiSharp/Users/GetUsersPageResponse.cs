using System.Text.Json.Serialization;
using XApiSharp.Common;
using XApiSharp.Errors;

namespace XApiSharp.Users;

/// <summary>Shared page shape for every Users-family operation that returns a page of
/// <see cref="User"/> (followers, following, blocking, muting, search, affiliates - all
/// "GetUsers*Response" schemas with an identical <c>{data, errors, includes, meta}</c> shape).</summary>
public sealed class GetUsersPageResponse : IXErrorCarryingResponse
{
    [JsonPropertyName("data")]
    public IReadOnlyList<User>? Data { get; init; }

    [JsonPropertyName("errors")]
    public IReadOnlyList<XProblem>? Errors { get; init; }

    [JsonPropertyName("includes")]
    public XIncludes? Includes { get; init; }

    [JsonPropertyName("meta")]
    public XPageMeta? Meta { get; init; }

    public bool HasErrors => Errors is { Count: > 0 };

    public bool IsPartialSuccess => Data is { Count: > 0 } && HasErrors;
}

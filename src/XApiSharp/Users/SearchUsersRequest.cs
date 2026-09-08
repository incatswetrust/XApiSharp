using XApiSharp.Common;

namespace XApiSharp.Users;

/// <summary>Request for <c>GET /2/users/search</c>. Uses <c>next_token</c>, not
/// <c>pagination_token</c> - a different continuation parameter name than every other Users
/// pagination operation (PAGE-02).</summary>
public sealed class SearchUsersRequest
{
    /// <summary>1-50 characters, per the registry.</summary>
    public required string Query { get; init; }

    public int? MaxResults { get; init; }

    /// <summary>PAGE-10: opaque - obtained from a previous page's <c>XPageMeta.NextToken</c>.</summary>
    public string? NextToken { get; init; }

    public IReadOnlyCollection<XUserField>? Fields { get; init; }

    public IReadOnlyCollection<XExpansion>? Expansions { get; init; }

    public IReadOnlyCollection<XPostField>? PostFields { get; init; }
}

namespace XApiSharp.Common;

/// <summary>The <c>community.fields</c> query parameter (SER-05).</summary>
public enum XCommunityField
{
    Access,
    CreatedAt,
    Description,
    Id,
    JoinPolicy,
    MemberCount,
    Name,
}

public static class XCommunityFieldExtensions
{
    public static string ToApiValue(this XCommunityField field) => field switch
    {
        XCommunityField.Access => "access",
        XCommunityField.CreatedAt => "created_at",
        XCommunityField.Description => "description",
        XCommunityField.Id => "id",
        XCommunityField.JoinPolicy => "join_policy",
        XCommunityField.MemberCount => "member_count",
        XCommunityField.Name => "name",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

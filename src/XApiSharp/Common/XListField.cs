namespace XApiSharp.Common;

/// <summary>The <c>list.fields</c> query parameter (SER-05) - selects which optional
/// <see cref="XList"/> fields to include in the response.</summary>
public enum XListField
{
    CreatedAt,
    Description,
    FollowerCount,
    Id,
    MemberCount,
    Name,
    Private,
}

public static class XListFieldExtensions
{
    public static string ToApiValue(this XListField field) => field switch
    {
        XListField.CreatedAt => "created_at",
        XListField.Description => "description",
        XListField.FollowerCount => "follower_count",
        XListField.Id => "id",
        XListField.MemberCount => "member_count",
        XListField.Name => "name",
        XListField.Private => "private",
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, message: null),
    };
}

namespace XApiSharp.Common;

/// <summary>The <c>expansions</c> query parameter on the Chat conversation operations - a small,
/// closed 3-value set distinct from the much larger <see cref="XExpansion"/> union used
/// elsewhere (PAGE-02).</summary>
public enum XChatExpansion
{
    AdminIds,
    MemberIds,
    ParticipantIds,
}

public static class XChatExpansionExtensions
{
    public static string ToApiValue(this XChatExpansion expansion) => expansion switch
    {
        XChatExpansion.AdminIds => "admin_ids",
        XChatExpansion.MemberIds => "member_ids",
        XChatExpansion.ParticipantIds => "participant_ids",
        _ => throw new ArgumentOutOfRangeException(nameof(expansion), expansion, message: null),
    };
}

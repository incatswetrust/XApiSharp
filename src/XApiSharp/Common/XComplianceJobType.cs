namespace XApiSharp.Common;

/// <summary>The <c>type</c> value on a Compliance Job - which resource kind the job covers.</summary>
public enum XComplianceJobType
{
    Tweets,
    Users,
}

public static class XComplianceJobTypeExtensions
{
    public static string ToApiValue(this XComplianceJobType value) => value switch
    {
        XComplianceJobType.Tweets => "tweets",
        XComplianceJobType.Users => "users",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

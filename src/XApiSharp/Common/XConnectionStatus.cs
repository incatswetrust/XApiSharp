namespace XApiSharp.Common;

/// <summary>The <c>status</c> filter on <c>GET /2/connections</c>.</summary>
public enum XConnectionStatus
{
    Active,
    Inactive,
    All,
}

public static class XConnectionStatusExtensions
{
    public static string ToApiValue(this XConnectionStatus value) => value switch
    {
        XConnectionStatus.Active => "active",
        XConnectionStatus.Inactive => "inactive",
        XConnectionStatus.All => "all",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

namespace XApiSharp.Common;

/// <summary>The <c>state</c> query parameter on <c>GET /2/spaces/search</c>.</summary>
public enum XSpaceState
{
    Live,
    Scheduled,
    All,
}

public static class XSpaceStateExtensions
{
    public static string ToApiValue(this XSpaceState value) => value switch
    {
        XSpaceState.Live => "live",
        XSpaceState.Scheduled => "scheduled",
        XSpaceState.All => "all",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

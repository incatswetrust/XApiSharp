namespace XApiSharp.Common;

/// <summary>The <c>dm_permission</c> value on <c>PUT /2/bots/{id}</c> - who can DM the bot.</summary>
public enum XBotDmAccess
{
    Everyone,
    Premium,
    NoOne,
}

public static class XBotDmAccessExtensions
{
    public static string ToApiValue(this XBotDmAccess value) => value switch
    {
        XBotDmAccess.Everyone => "everyone",
        XBotDmAccess.Premium => "premium",
        XBotDmAccess.NoOne => "no_one",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, message: null),
    };
}

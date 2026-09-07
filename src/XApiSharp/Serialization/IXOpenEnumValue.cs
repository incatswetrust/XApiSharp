namespace XApiSharp.Serialization;

/// <summary>
/// SER-06: contract for "extensible string enum" wrapper types - a new value X adds server-side
/// must round-trip through the SDK untouched instead of throwing, which a closed C# <c>enum</c>
/// with a strict converter would do. Implementations are typically a readonly struct wrapping a
/// string, with a small set of known static values plus implicit round-tripping of anything else.
/// </summary>
public interface IXOpenEnumValue<TSelf> where TSelf : struct, IXOpenEnumValue<TSelf>
{
    string Value { get; }

    static abstract TSelf FromValue(string value);
}

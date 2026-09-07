using System.Text.Json.Serialization;
using XApiSharp.Serialization;

namespace XApiSharp.Users;

/// <summary>The X Blue verified type of a user (SER-06 open enum - see
/// <see cref="IXOpenEnumValue{TSelf}"/>). Known values per current docs; an unrecognized value from
/// X still round-trips via <see cref="Value"/> instead of throwing.</summary>
[JsonConverter(typeof(XOpenEnumJsonConverter<VerifiedType>))]
public readonly struct VerifiedType : IXOpenEnumValue<VerifiedType>, IEquatable<VerifiedType>
{
    private VerifiedType(string value) => Value = value;

    public string Value { get; }

    public static VerifiedType FromValue(string value) => new(value);

    public static VerifiedType Blue { get; } = new("blue");

    public static VerifiedType Government { get; } = new("government");

    public static VerifiedType Business { get; } = new("business");

    public static VerifiedType None { get; } = new("none");

    public bool Equals(VerifiedType other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is VerifiedType other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    public static bool operator ==(VerifiedType left, VerifiedType right) => left.Equals(right);

    public static bool operator !=(VerifiedType left, VerifiedType right) => !left.Equals(right);
}

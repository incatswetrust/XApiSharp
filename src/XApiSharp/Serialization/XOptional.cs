using System.Text.Json.Serialization;

namespace XApiSharp.Serialization;

/// <summary>
/// SER-03: distinguishes "field not set" (omit from the JSON body entirely) from "field
/// explicitly set to null" from "field set to a value" - plain nullable types can't tell the
/// first two apart, which matters wherever the server treats "absent" and "null" differently
/// (e.g. partial-update semantics). No current request model needs this yet (the one endpoint
/// implemented so far, GetUserRequest, has only a required Id) - this is core infrastructure for
/// the optional `fields`/`expansions`-style request parameters landing with the rest of the
/// Users family in E4.
/// </summary>
/// <remarks>
/// Usage on a request model:
/// <code>
/// [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
/// public XOptional&lt;string&gt; DisplayName { get; init; }
/// </code>
/// Leaving the property unassigned keeps it at <c>default(XOptional&lt;T&gt;)</c> (<see cref="IsSet"/>
/// false), which <c>WhenWritingDefault</c> omits from the JSON entirely. Assign
/// <c>XOptional.Of&lt;T&gt;(null)</c> to send an explicit JSON <c>null</c>, or assign a value
/// (implicit conversion from <typeparamref name="T"/>) to send that value.
/// </remarks>
[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct XOptional<T> : IEquatable<XOptional<T>>
{
    internal XOptional(bool isSet, T? value)
    {
        IsSet = isSet;
        Value = value;
    }

    public bool IsSet { get; }

    public T? Value { get; }

    public static implicit operator XOptional<T>(T? value) => XOptional.Of(value);

    public bool Equals(XOptional<T> other) => IsSet == other.IsSet && EqualityComparer<T?>.Default.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is XOptional<T> other && Equals(other);

    public override int GetHashCode() => IsSet ? HashCode.Combine(IsSet, Value) : 0;

    public static bool operator ==(XOptional<T> left, XOptional<T> right) => left.Equals(right);

    public static bool operator !=(XOptional<T> left, XOptional<T> right) => !left.Equals(right);
}

/// <summary>Non-generic factory so <see cref="XOptional{T}"/> doesn't declare static members on
/// a generic type (CA1000).</summary>
public static class XOptional
{
    public static XOptional<T> Of<T>(T? value) => new(isSet: true, value);
}

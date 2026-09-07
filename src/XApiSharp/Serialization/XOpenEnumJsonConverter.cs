using System.Text.Json;
using System.Text.Json.Serialization;

namespace XApiSharp.Serialization;

/// <summary>Reads/writes an <see cref="IXOpenEnumValue{TSelf}"/> as its raw JSON string, so an
/// unrecognized value round-trips instead of failing deserialization (SER-06).</summary>
public sealed class XOpenEnumJsonConverter<T> : JsonConverter<T> where T : struct, IXOpenEnumValue<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException($"Expected a string value for {typeToConvert.Name}.");
        return T.FromValue(value);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

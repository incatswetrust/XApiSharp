using System.Text.Json;
using System.Text.Json.Serialization;
using XApiSharp.Serialization;

namespace XApiSharp.UnitTests;

public class XOptionalTests
{
    private sealed class TestRequest
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public XOptional<string> Name { get; init; }
    }

    [Fact]
    public void Unset_field_is_omitted_from_the_json_body()
    {
        var request = new TestRequest();

        var json = JsonSerializer.Serialize(request);

        Assert.Equal("{}", json);
    }

    [Fact]
    public void Explicit_null_serializes_as_json_null()
    {
        var request = new TestRequest { Name = XOptional.Of<string>(null) };

        var json = JsonSerializer.Serialize(request);

        Assert.Equal("""{"Name":null}""", json);
    }

    [Fact]
    public void Value_serializes_normally()
    {
        var request = new TestRequest { Name = "Ada" };

        var json = JsonSerializer.Serialize(request);

        Assert.Equal("""{"Name":"Ada"}""", json);
    }

    [Fact]
    public void Deserializing_a_present_value_sets_IsSet_true()
    {
        var request = JsonSerializer.Deserialize<TestRequest>("""{"Name":"Ada"}""");

        Assert.True(request!.Name.IsSet);
        Assert.Equal("Ada", request.Name.Value);
    }

    [Fact]
    public void Deserializing_an_explicit_null_sets_IsSet_true_with_a_null_value()
    {
        var request = JsonSerializer.Deserialize<TestRequest>("""{"Name":null}""");

        Assert.True(request!.Name.IsSet);
        Assert.Null(request.Name.Value);
    }

    [Fact]
    public void Deserializing_an_absent_field_leaves_it_unset()
    {
        var request = JsonSerializer.Deserialize<TestRequest>("{}");

        Assert.False(request!.Name.IsSet);
    }

    [Fact]
    public void Equality_considers_both_IsSet_and_Value()
    {
        Assert.Equal(default, default(XOptional<string>));
        Assert.NotEqual(default, XOptional.Of<string>(null));
        Assert.Equal(XOptional.Of("a"), XOptional.Of("a"));
        Assert.NotEqual(XOptional.Of("a"), XOptional.Of("b"));
    }
}

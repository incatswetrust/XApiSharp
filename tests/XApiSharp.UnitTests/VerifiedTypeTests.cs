using System.Text.Json;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

/// <summary>SER-06: an open enum preserves an unrecognized value instead of throwing.</summary>
public class VerifiedTypeTests
{
    [Fact]
    public void Known_value_deserializes_and_equals_the_static_constant()
    {
        var value = JsonSerializer.Deserialize<VerifiedType>("\"blue\"");

        Assert.Equal(VerifiedType.Blue, value);
    }

    [Fact]
    public void Unrecognized_value_round_trips_instead_of_throwing()
    {
        var value = JsonSerializer.Deserialize<VerifiedType>("\"a_new_tier_x_added_later\"");

        Assert.Equal("a_new_tier_x_added_later", value.Value);
        Assert.NotEqual(VerifiedType.Blue, value);

        var json = JsonSerializer.Serialize(value);
        Assert.Equal("\"a_new_tier_x_added_later\"", json);
    }

    [Fact]
    public void User_with_an_unknown_verified_type_still_deserializes_fully()
    {
        const string json = """{"id":"1","name":"A","username":"a","verified_type":"brand_new"}""";

        var user = JsonSerializer.Deserialize<User>(json);

        Assert.Equal("brand_new", user!.VerifiedType?.Value);
        Assert.Equal("1", user.Id);
    }
}

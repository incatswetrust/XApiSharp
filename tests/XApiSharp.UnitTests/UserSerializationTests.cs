using System.Text.Json;
using XApiSharp.Users;

namespace XApiSharp.UnitTests;

public class UserSerializationTests
{
    [Fact]
    public void CreatedAt_parses_the_documented_ISO8601_millisecond_format()
    {
        // SER-02: X's actual created_at format, e.g. "2013-12-14T04:35:55.000Z".
        const string json = """{"id":"1","name":"A","username":"a","created_at":"2013-12-14T04:35:55.000Z"}""";

        var user = JsonSerializer.Deserialize<User>(json);

        Assert.Equal(
            new DateTimeOffset(2013, 12, 14, 4, 35, 55, TimeSpan.Zero),
            user!.CreatedAt);
    }
}

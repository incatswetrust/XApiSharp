using XApiSharp.Transport;

namespace XApiSharp.UnitTests;

/// <summary>SER-10: query-string construction - single escaping, comma-joined arrays, no
/// parameter emitted for an absent value.</summary>
public class QueryStringBuilderTests
{
    [Fact]
    public void Returns_null_when_every_value_is_null_or_empty()
    {
        var result = QueryStringBuilder.Build([("a", null), ("b", "")]);

        Assert.Null(result);
    }

    [Fact]
    public void Escapes_each_value_exactly_once()
    {
        var result = QueryStringBuilder.Build([("query", "a b&c=d")]);

        Assert.Equal("?query=a%20b%26c%3Dd", result);
        // If this were double-escaped, the literal "%20" would itself become "%2520".
        Assert.DoesNotContain("%25", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Joins_multiple_parameters_with_ampersand_skipping_absent_ones()
    {
        var result = QueryStringBuilder.Build([("a", "1"), ("b", null), ("c", "3")]);

        Assert.Equal("?a=1&c=3", result);
    }

    [Fact]
    public void JoinCommaSeparated_uses_the_documented_array_convention()
    {
        var result = QueryStringBuilder.JoinCommaSeparated(["id", "name", "username"]);

        Assert.Equal("id,name,username", result);
    }

    [Fact]
    public void JoinCommaSeparated_returns_null_for_an_empty_collection()
    {
        Assert.Null(QueryStringBuilder.JoinCommaSeparated([]));
        Assert.Null(QueryStringBuilder.JoinCommaSeparated(null));
    }
}

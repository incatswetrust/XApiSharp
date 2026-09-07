namespace XApiSharp.UnitTests;

public class XRateLimitInfoTests
{
    [Fact]
    public void FromHeaders_parses_present_headers()
    {
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("x-rate-limit-limit", "100");
        response.Headers.TryAddWithoutValidation("x-rate-limit-remaining", "42");
        response.Headers.TryAddWithoutValidation("x-rate-limit-reset", "1735689600");

        var info = XRateLimitInfo.FromHeaders(response.Headers);

        Assert.NotNull(info);
        Assert.Equal(100, info!.Limit);
        Assert.Equal(42, info.Remaining);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1735689600), info.Reset);
    }

    [Fact]
    public void FromHeaders_returns_null_when_no_rate_limit_headers_present()
    {
        using var response = new HttpResponseMessage();

        var info = XRateLimitInfo.FromHeaders(response.Headers);

        Assert.Null(info);
    }

    [Fact]
    public void FromHeaders_does_not_treat_an_unparsable_value_as_zero()
    {
        // RATE-02: an unparsable header must not silently become 0 or "unlimited".
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("x-rate-limit-remaining", "not-a-number");
        response.Headers.TryAddWithoutValidation("x-rate-limit-limit", "100");

        var info = XRateLimitInfo.FromHeaders(response.Headers);

        Assert.NotNull(info);
        Assert.Equal(100, info!.Limit);
        Assert.Null(info.Remaining);
    }
}

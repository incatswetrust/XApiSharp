using XApiSharp.Authentication;

namespace XApiSharp.UnitTests;

public class XApiClientTests
{
    [Fact]
    public void Constructor_throws_for_null_http_client()
    {
        var auth = new BearerTokenAuthenticationProvider("token");

        Assert.Throws<ArgumentNullException>(() => new XApiClient(null!, auth));
    }

    [Fact]
    public void Constructor_throws_for_null_authentication_provider()
    {
        using var httpClient = new HttpClient();

        Assert.Throws<ArgumentNullException>(() => new XApiClient(httpClient, null!));
    }

    [Fact]
    public void Constructor_uses_default_options_when_none_provided()
    {
        using var httpClient = new HttpClient();
        var auth = new BearerTokenAuthenticationProvider("token");

        var client = new XApiClient(httpClient, auth);

        Assert.NotNull(client.Users);
    }

    [Fact]
    public void Default_base_url_is_the_official_x_api_host()
    {
        var options = new XClientOptions();

        Assert.Equal(new Uri("https://api.x.com"), options.BaseUrl);
    }
}

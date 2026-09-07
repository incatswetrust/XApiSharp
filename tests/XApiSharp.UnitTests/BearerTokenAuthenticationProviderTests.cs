using XApiSharp.Authentication;

namespace XApiSharp.UnitTests;

public class BearerTokenAuthenticationProviderTests
{
    [Fact]
    public async Task PrepareRequestAsync_sets_bearer_authorization_header()
    {
        var provider = new BearerTokenAuthenticationProvider("abc123");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.x.com/2/users/1");

        await provider.PrepareRequestAsync(request, CancellationToken.None);

        Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
        Assert.Equal("abc123", request.Headers.Authorization?.Parameter);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_empty_or_whitespace_token(string token)
    {
        Assert.Throws<ArgumentException>(() => new BearerTokenAuthenticationProvider(token));
    }

    [Fact]
    public void Constructor_rejects_null_token()
    {
        Assert.Throws<ArgumentNullException>(() => new BearerTokenAuthenticationProvider(null!));
    }
}

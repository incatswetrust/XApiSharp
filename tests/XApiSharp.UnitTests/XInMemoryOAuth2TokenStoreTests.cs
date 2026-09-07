using XApiSharp.Authentication;

namespace XApiSharp.UnitTests;

public class XInMemoryOAuth2TokenStoreTests
{
    [Fact]
    public async Task GetAsync_returns_null_before_anything_is_set()
    {
        var store = new XInMemoryOAuth2TokenStore();

        Assert.Null(await store.GetAsync(CancellationToken.None));
    }

    [Fact]
    public async Task First_write_requires_null_expected_version()
    {
        var store = new XInMemoryOAuth2TokenStore();

        var ok = await store.TrySetAsync("token", null, DateTimeOffset.UtcNow.AddHours(1), expectedVersion: null, CancellationToken.None);

        Assert.True(ok);
        var stored = await store.GetAsync(CancellationToken.None);
        Assert.Equal("token", stored!.AccessToken);
    }

    [Fact]
    public async Task Write_with_stale_expected_version_is_rejected()
    {
        var store = new XInMemoryOAuth2TokenStore();
        await store.TrySetAsync("token-1", null, DateTimeOffset.UtcNow.AddHours(1), expectedVersion: null, CancellationToken.None);

        var ok = await store.TrySetAsync("token-2", null, DateTimeOffset.UtcNow.AddHours(1), expectedVersion: "not-the-real-version", CancellationToken.None);

        Assert.False(ok);
        var stored = await store.GetAsync(CancellationToken.None);
        Assert.Equal("token-1", stored!.AccessToken); // unchanged
    }

    [Fact]
    public async Task Write_with_the_correct_current_version_succeeds_and_changes_the_version()
    {
        var store = new XInMemoryOAuth2TokenStore();
        await store.TrySetAsync("token-1", null, DateTimeOffset.UtcNow.AddHours(1), expectedVersion: null, CancellationToken.None);
        var first = await store.GetAsync(CancellationToken.None);

        var ok = await store.TrySetAsync("token-2", null, DateTimeOffset.UtcNow.AddHours(1), first!.Version, CancellationToken.None);

        Assert.True(ok);
        var second = await store.GetAsync(CancellationToken.None);
        Assert.Equal("token-2", second!.AccessToken);
        Assert.NotEqual(first.Version, second.Version);
    }
}

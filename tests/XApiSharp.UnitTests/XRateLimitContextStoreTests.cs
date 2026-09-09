using XApiSharp.Authentication;
using XApiSharp.Transport;

namespace XApiSharp.UnitTests;

/// <summary>
/// Direct tests of the persistence/ordering/scoping logic (RATE-03/04/05) - deterministic and
/// timing-independent, since they exercise <see cref="XRateLimitContextStore"/>/
/// <see cref="XRateLimitStateRegistry"/> directly rather than through real HTTP calls. The
/// proactive-wait *behavior* built on top of this (consulting the store before sending, waiting
/// for a known reset) is covered separately in <c>RateLimitPersistenceTests</c>, since that needs
/// the full <see cref="XApiClient"/>/<see cref="Transport.RequestExecutor"/> path.
/// </summary>
public class XRateLimitContextStoreTests
{
    [Fact]
    public void TryGet_returns_null_for_a_scope_nothing_has_been_recorded_for()
    {
        var store = new XRateLimitContextStore();
        Assert.Null(store.TryGet("GET 2/users/{id}"));
    }

    [Fact]
    public void Record_then_TryGet_round_trips_the_same_info()
    {
        var store = new XRateLimitContextStore();
        var info = new XRateLimitInfo { Limit = 15, Remaining = 3, Reset = DateTimeOffset.UtcNow.AddMinutes(1) };

        store.Record("GET 2/users/{id}", store.NextSequence(), info);

        Assert.Same(info, store.TryGet("GET 2/users/{id}"));
    }

    [Fact]
    public void A_later_sequenced_write_overwrites_an_earlier_one_for_the_same_scope()
    {
        var store = new XRateLimitContextStore();
        var older = new XRateLimitInfo { Remaining = 8, Reset = DateTimeOffset.UtcNow.AddMinutes(5) };
        var newer = new XRateLimitInfo { Remaining = 5, Reset = DateTimeOffset.UtcNow.AddMinutes(5) };

        store.Record("GET 2/users/{id}", 1, older);
        store.Record("GET 2/users/{id}", 2, newer);

        Assert.Same(newer, store.TryGet("GET 2/users/{id}"));
    }

    [Fact]
    public void RATE05_a_lower_sequence_write_arriving_after_a_higher_one_does_not_restore_the_older_value()
    {
        var store = new XRateLimitContextStore();
        var newer = new XRateLimitInfo { Remaining = 5, Reset = DateTimeOffset.UtcNow.AddMinutes(5) };
        var staleLateArrival = new XRateLimitInfo { Remaining = 8, Reset = DateTimeOffset.UtcNow.AddMinutes(5) };

        // Sequence 2 (the "more recent" send) is recorded first, simulating its response
        // completing before sequence 1's - a genuinely late-arriving response from an
        // earlier-started attempt must not overwrite it.
        store.Record("GET 2/users/{id}", 2, newer);
        store.Record("GET 2/users/{id}", 1, staleLateArrival);

        Assert.Same(newer, store.TryGet("GET 2/users/{id}"));
    }

    [Fact]
    public void RATE03_different_scope_keys_never_interfere()
    {
        var store = new XRateLimitContextStore();
        var usersInfo = new XRateLimitInfo { Remaining = 0, Reset = DateTimeOffset.UtcNow.AddMinutes(5) };
        var postsInfo = new XRateLimitInfo { Remaining = 50, Reset = DateTimeOffset.UtcNow.AddMinutes(5) };

        store.Record("GET 2/users/{id}", store.NextSequence(), usersInfo);
        store.Record("POST 2/tweets", store.NextSequence(), postsInfo);

        Assert.Same(usersInfo, store.TryGet("GET 2/users/{id}"));
        Assert.Same(postsInfo, store.TryGet("POST 2/tweets"));
    }

    [Fact]
    public void NextSequence_is_monotonically_increasing()
    {
        var store = new XRateLimitContextStore();
        var first = store.NextSequence();
        var second = store.NextSequence();
        Assert.True(second > first);
    }

    [Fact]
    public void RATE04_the_registry_gives_different_auth_provider_instances_independent_stores()
    {
        var providerA = new BearerTokenAuthenticationProvider("token-a");
        var providerB = new BearerTokenAuthenticationProvider("token-b");

        var storeA = XRateLimitStateRegistry.GetOrCreate(providerA);
        var storeB = XRateLimitStateRegistry.GetOrCreate(providerB);

        Assert.NotSame(storeA, storeB);

        storeA.Record("GET 2/users/{id}", storeA.NextSequence(), new XRateLimitInfo { Remaining = 0, Reset = DateTimeOffset.UtcNow.AddMinutes(5) });

        Assert.Null(storeB.TryGet("GET 2/users/{id}"));
    }

    [Fact]
    public void The_registry_returns_the_same_store_for_the_same_provider_instance_every_time()
    {
        var provider = new BearerTokenAuthenticationProvider("token");

        var first = XRateLimitStateRegistry.GetOrCreate(provider);
        var second = XRateLimitStateRegistry.GetOrCreate(provider);

        Assert.Same(first, second);
    }
}

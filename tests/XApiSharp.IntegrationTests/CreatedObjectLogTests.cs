using Xunit.Abstractions;

namespace XApiSharp.IntegrationTests;

/// <summary>
/// Deterministic coverage for <see cref="CreatedObjectLog"/> itself - the piece of spec 19.4's
/// fixture/cleanup policy that can be verified without live X API access (unlike the actual live
/// scenarios, which need real credentials this project doesn't have yet).
/// </summary>
public class CreatedObjectLogTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Tracked_objects_are_deleted_in_reverse_creation_order()
    {
        var deleted = new List<string>();
        await using (var log = new CreatedObjectLog(output))
        {
            log.Track("Post", "1", (id, _) => { deleted.Add(id); return Task.CompletedTask; });
            log.Track("Post", "2", (id, _) => { deleted.Add(id); return Task.CompletedTask; });
            log.Track("Post", "3", (id, _) => { deleted.Add(id); return Task.CompletedTask; });

            Assert.Equal([("Post", "1"), ("Post", "2"), ("Post", "3")], log.Created);
        }

        // Newest-first, in case a later object depends on an earlier one.
        Assert.Equal(["3", "2", "1"], deleted);
    }

    [Fact]
    public async Task A_failed_delete_does_not_stop_the_remaining_objects_from_being_cleaned_up()
    {
        var deleted = new List<string>();
        await using (var log = new CreatedObjectLog(output))
        {
            log.Track("Post", "1", (id, _) => { deleted.Add(id); return Task.CompletedTask; });
            log.Track("Post", "2", (_, _) => throw new InvalidOperationException("simulated delete failure"));
            log.Track("Post", "3", (id, _) => { deleted.Add(id); return Task.CompletedTask; });
        }

        // "2" failed and is absent from `deleted`, but "1" and "3" (deleted first, in reverse
        // order) were not skipped because of it.
        Assert.Equal(["3", "1"], deleted);
    }

    [Fact]
    public void Track_rejects_an_empty_family_or_id()
    {
        var log = new CreatedObjectLog(output);

        Assert.Throws<ArgumentException>(() => log.Track("", "1", (_, _) => Task.CompletedTask));
        Assert.Throws<ArgumentException>(() => log.Track("Post", "", (_, _) => Task.CompletedTask));
    }
}

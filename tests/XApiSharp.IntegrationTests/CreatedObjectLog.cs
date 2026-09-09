using Xunit.Abstractions;

namespace XApiSharp.IntegrationTests;

/// <summary>
/// Spec section 19.4's fixture/cleanup policy as reusable infrastructure, not only prose: "cleanup
/// deletes only objects created by this test run; the IDs of such objects are recorded in the run
/// log." A live test tracks every object it creates immediately after the create call succeeds -
/// before any further assertions that might throw - so a failed test still cleans up what it made,
/// and cleanup only ever touches objects this run is actually responsible for.
/// </summary>
public sealed class CreatedObjectLog(ITestOutputHelper output) : IAsyncDisposable
{
    private readonly List<(string Family, string Id, Func<string, CancellationToken, Task> Delete)> _created = [];

    /// <summary>Records one created object and how to delete it. Call this right after the create
    /// call returns successfully - not batched at the end, since a test that fails partway through
    /// must still have logged (and therefore be able to clean up) everything created so far.</summary>
    public void Track(string family, string id, Func<string, CancellationToken, Task> delete)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(delete);

        _created.Add((family, id, delete));
        output.WriteLine($"[fixture] created {family} {id}");
    }

    /// <summary>Every tracked object this run created, oldest first - exposed so a test can assert
    /// on what got logged, and so the ID list is available even if cleanup itself fails.</summary>
    public IReadOnlyList<(string Family, string Id)> Created => _created.Select(c => (c.Family, c.Id)).ToList();

    public async ValueTask DisposeAsync()
    {
        // Newest-first: a later-created object may depend on an earlier one (e.g. a reply Post on
        // its parent), so deleting in reverse creation order avoids a foreign-key-style failure.
        for (var i = _created.Count - 1; i >= 0; i--)
        {
            var (family, id, delete) = _created[i];
            try
            {
                await delete(id, CancellationToken.None).ConfigureAwait(false);
                output.WriteLine($"[fixture] deleted {family} {id}");
            }
            catch (Exception ex)
            {
                // Best-effort: one failed delete must not hide the test's own failure, and must
                // not stop the rest of this run's objects from being cleaned up too - but it is
                // logged, not swallowed, so a real leak in the test X app stays visible.
                output.WriteLine($"[fixture] FAILED to delete {family} {id}: {ex.Message}");
            }
        }
    }
}

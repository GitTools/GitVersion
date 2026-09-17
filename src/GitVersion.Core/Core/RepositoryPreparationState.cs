namespace GitVersion;

// Shared per-execution preparation results. Reading these results must not
// require constructing the preparer or a mutable repository adapter.
internal sealed class RepositoryPreparationState
{
    public string? FetchedRemoteName { get; set; }
}

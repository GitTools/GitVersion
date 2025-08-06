using LibGit2Sharp;

namespace GitVersion.Testing;

/// <summary>
///     Creates a remote repository then clones it
///     Remote = Repository
///     Local  = LocalRepositoryFixture
/// </summary>
public class RemoteRepositoryFixture : RepositoryFixtureBase
{
    public RemoteRepositoryFixture(Func<string, Repository> builder)
        : base(builder) => LocalRepositoryFixture = CloneRepository();

    public RemoteRepositoryFixture(string branchName = MainBranch)
        : this(path => CreateNewRepository(path, branchName, 5))
    {
    }

    /// <summary>
    ///     Fixture pointing at the local repository
    /// </summary>
    public LocalRepositoryFixture LocalRepositoryFixture { get; }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LocalRepositoryFixture.Dispose();
        }
        base.Dispose(disposing);
    }
}

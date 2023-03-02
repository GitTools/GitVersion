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
        : base(builder) => CreateLocalRepository();

    public RemoteRepositoryFixture() : this("main")
    {
    }

    public RemoteRepositoryFixture(string branchName)
        : this(path => CreateNewRepository(path, branchName))
    {
    }

    /// <summary>
    ///     Fixture pointing at the local repository
    /// </summary>
    public LocalRepositoryFixture LocalRepositoryFixture { get; private set; }

    private static Repository CreateNewRepository(string path, string branchName)
    {
        Init(path, branchName);
        Console.WriteLine("Created git repository at '{0}'", path);

        var repository = new Repository(path);
        repository.MakeCommits(5);
        return repository;
    }

    private void CreateLocalRepository() => LocalRepositoryFixture = CloneRepository();

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

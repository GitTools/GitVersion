using LibGit2Sharp;

namespace GitTools.Testing;

/// <summary>
///     Creates a remote repository then clones it
///     Remote = Repository
///     Local  = LocalRepositoryFixture
/// </summary>
public class RemoteRepositoryFixture : RepositoryFixtureBase
{
    public RemoteRepositoryFixture(Func<string, IRepository> builder)
        : base(builder) => CreateLocalRepository();

    public RemoteRepositoryFixture() : this(CreateNewRepository)
    {
    }

    /// <summary>
    ///     Fixture pointing at the local repository
    /// </summary>
    public LocalRepositoryFixture LocalRepositoryFixture { get; private set; }

    private static IRepository CreateNewRepository(string path)
    {
        Init(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        var repo = new Repository(path);
        repo.MakeCommits(5);
        return repo;
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

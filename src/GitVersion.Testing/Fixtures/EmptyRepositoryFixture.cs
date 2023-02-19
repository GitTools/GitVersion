using LibGit2Sharp;

namespace GitVersion.Testing;

public class EmptyRepositoryFixture : RepositoryFixtureBase
{
    public EmptyRepositoryFixture() : this("main")
    {
    }

    public EmptyRepositoryFixture(string branchName)
        : base(path => CreateNewRepository(path, branchName))
    {
    }

    private static IRepository CreateNewRepository(string path, string branchName)
    {
        Init(path, branchName);
        Console.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}

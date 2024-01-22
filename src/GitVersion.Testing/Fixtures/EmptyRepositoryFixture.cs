using LibGit2Sharp;

namespace GitVersion.Testing;

public class EmptyRepositoryFixture(string branchName) : RepositoryFixtureBase(path => CreateNewRepository(path, branchName))
{
    public EmptyRepositoryFixture() : this("main")
    {
    }

    private static Repository CreateNewRepository(string path, string branchName)
    {
        Init(path, branchName);
        Console.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}

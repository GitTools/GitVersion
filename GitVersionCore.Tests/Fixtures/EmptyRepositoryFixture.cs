using System.Diagnostics;
using GitVersion;
using LibGit2Sharp;

public class EmptyRepositoryFixture : RepositoryFixtureBase
{
    public EmptyRepositoryFixture(Config configuration) :
        base(CreateNewRepository, configuration)
    {
    }

    public void DumpGraph()
    {
        Repository.DumpGraph();
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path);
        Trace.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}
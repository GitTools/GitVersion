using System;
using GitVersion;
using LibGit2Sharp;

public class EmptyRepositoryFixture : RepositoryFixtureBase
{
    public EmptyRepositoryFixture(Config configuration) :
        base(CreateNewRepository, configuration)
    {
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}
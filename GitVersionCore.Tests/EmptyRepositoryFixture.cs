using System;
using GitVersion.Configuration;
using LibGit2Sharp;

public class EmptyRepositoryFixture : RepositoryFixtureBase
{
    public EmptyRepositoryFixture(Config config) :
        base(CreateNewRepository, config)
    {
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        return new Repository(path);
    }
}
using System;
using GitVersion;
using LibGit2Sharp;

public class RemoteRepositoryFixture : RepositoryFixtureBase
{
    public RemoteRepositoryFixture(Func<string, IRepository> builder, Config configuration)
        : base(builder, configuration)
    {
        CreateLocalRepository();
    }

    public RemoteRepositoryFixture(Config configuration)
        : base(CreateNewRepository, configuration)
    {
        CreateLocalRepository();
    }

    public LocalRepositoryFixture LocalRepositoryFixture { get; private set; }


    /// <summary>
    /// Simulates running on build server
    /// </summary>
    public void InitialiseRepo()
    {
        new GitPreparer(null, null, new Authentication(), null, false, LocalRepositoryFixture.RepositoryPath).Initialise(true, null);
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        var repo = new Repository(path);
        repo.MakeCommits(5);
        return repo;
    }

    void CreateLocalRepository()
    {
        LocalRepositoryFixture = CloneRepository();
    }

    public override void Dispose()
    {
        LocalRepositoryFixture.Dispose();
        base.Dispose();
    }
}
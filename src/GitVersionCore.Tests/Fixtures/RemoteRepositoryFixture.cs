using System;

    using GitVersion;
    using LibGit2Sharp;

public class RemoteRepositoryFixture : RepositoryFixtureBase
{
    public string LocalRepositoryPath;
    public IRepository LocalRepository;

    public RemoteRepositoryFixture(Func<string, IRepository> builder, Config configuration)
        : base(builder, configuration)
    {
        CloneRepository();
    }

    public RemoteRepositoryFixture(Config configuration)
        : base(CreateNewRepository, configuration)
    {
        CloneRepository();
    }

    /// <summary>
    /// Simulates running on build server
    /// </summary>
    public void InitialiseRepo()
    {
        new GitPreparer(null, null, new Authentication(), null, false, LocalRepositoryPath).Initialise(true, null);
    }

    static IRepository CreateNewRepository(string path)
    {
        LibGit2Sharp.Repository.Init(path);
        Console.WriteLine("Created git repository at '{0}'", path);

        var repo = new Repository(path);
        repo.MakeCommits(5);
        return repo;
    }

    void CloneRepository()
    {
        LocalRepositoryPath = PathHelper.GetTempPath();
        LibGit2Sharp.Repository.Clone(RepositoryPath, LocalRepositoryPath);
        LocalRepository = new Repository(LocalRepositoryPath);
    }

    public override void Dispose()
    {
        LocalRepository.Dispose();
        try
        {
            DirectoryHelper.DeleteDirectory(LocalRepositoryPath);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath, e.Message);
        }

        base.Dispose();
    }
}
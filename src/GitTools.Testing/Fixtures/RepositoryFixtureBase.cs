using GitTools.Testing.Internal;
using LibGit2Sharp;

namespace GitTools.Testing;

/// <summary>
///     Fixture abstracting a git repository
/// </summary>
public abstract class RepositoryFixtureBase : IDisposable
{

    protected RepositoryFixtureBase(Func<string, IRepository> repoBuilder)
        : this(repoBuilder(PathHelper.GetTempPath()))
    {
    }

    protected RepositoryFixtureBase(IRepository repository)
    {
        this.SequenceDiagram = new SequenceDiagram();
        Repository = repository;
        Repository.Config.Set("user.name", "Test");
        Repository.Config.Set("user.email", "test@email.com");
    }

    public IRepository Repository { get; }

    public string RepositoryPath => Repository.Info.WorkingDirectory.TrimEnd('\\');

    public SequenceDiagram SequenceDiagram { get; }


    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Repository.Dispose();

        try
        {
            DirectoryHelper.DeleteDirectory(RepositoryPath);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath,
                e.Message);
        }

        this.SequenceDiagram.End();
        Console.WriteLine("**Visualisation of test:**");
        Console.WriteLine(string.Empty);
        Console.WriteLine(this.SequenceDiagram.GetDiagram());
    }

    public void Checkout(string branch) => Commands.Checkout(Repository, branch);

    public static void Init(string path) => GitTestExtensions.ExecuteGitCmd($"init {path} -b main");

    public void MakeATaggedCommit(string tag)
    {
        MakeACommit();
        ApplyTag(tag);
    }

    public void ApplyTag(string tag)
    {
        this.SequenceDiagram.ApplyTag(tag, Repository.Head.FriendlyName);
        Repository.ApplyTag(tag);
    }

    public void BranchTo(string branchName, string? @as = null)
    {
        this.SequenceDiagram.BranchTo(branchName, Repository.Head.FriendlyName, @as);
        var branch = Repository.CreateBranch(branchName);
        Commands.Checkout(Repository, branch);
    }

    public void BranchToFromTag(string branchName, string fromTag, string onBranch, string? @as = null)
    {
        this.SequenceDiagram.BranchToFromTag(branchName, fromTag, onBranch, @as);
        var branch = Repository.CreateBranch(branchName);
        Commands.Checkout(Repository, branch);
    }

    public void MakeACommit()
    {
        var to = Repository.Head.FriendlyName;
        this.SequenceDiagram.MakeACommit(to);
        Repository.MakeACommit();
    }

    /// <summary>
    ///     Merges (no-ff) specified branch into the current HEAD of this repository
    /// </summary>
    public void MergeNoFF(string mergeSource)
    {
        this.SequenceDiagram.Merge(mergeSource, Repository.Head.FriendlyName);
        Repository.MergeNoFF(mergeSource, Generate.SignatureNow());
    }

    /// <summary>
    ///     Clones the repository managed by this fixture into another LocalRepositoryFixture
    /// </summary>
    public LocalRepositoryFixture CloneRepository()
    {
        var localPath = PathHelper.GetTempPath();
        LibGit2Sharp.Repository.Clone(RepositoryPath, localPath);
        return new LocalRepositoryFixture(new Repository(localPath));
    }
}

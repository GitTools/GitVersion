using GitVersion.Helpers;
using GitVersion.Testing.Internal;
using LibGit2Sharp;

namespace GitVersion.Testing;

/// <summary>
///     Fixture abstracting a git repository
/// </summary>
public abstract class RepositoryFixtureBase : IDisposable
{
    protected RepositoryFixtureBase(Func<string, Repository> repositoryBuilder)
        : this(repositoryBuilder(PathHelper.GetTempPath()))
    {
    }

    protected RepositoryFixtureBase(Repository repository)
    {
        this.SequenceDiagram = new SequenceDiagram();
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Repository.Config.Set("user.name", "Test");
        Repository.Config.Set("user.email", "test@email.com");
    }

    public Repository Repository { get; }

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

    public void Remove(string branch) => Repository.Branches.Remove(branch);

    public static void Init(string path, string branchName) => GitTestExtensions.ExecuteGitCmd($"init {path} -b {branchName}");

    public string MakeATaggedCommit(string tag)
    {
        var sha = MakeACommit();
        ApplyTag(tag);
        return sha;
    }

    public void ApplyTag(string tag)
    {
        this.SequenceDiagram.ApplyTag(tag, Repository.Head.FriendlyName);
        Repository.ApplyTag(tag);
    }

    public void CreateBranch(string branchName, string? @as = null)
    {
        this.SequenceDiagram.BranchTo(branchName, Repository.Head.FriendlyName, @as);
        Repository.CreateBranch(branchName);
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

    public string MakeACommit()
    {
        var to = Repository.Head.FriendlyName;
        this.SequenceDiagram.MakeACommit(to);
        var commit = Repository.MakeACommit();
        return commit.Sha;
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
        Repository.Clone(RepositoryPath, localPath);
        return new LocalRepositoryFixture(new Repository(localPath));
    }

    /// <summary>
    ///     Pulls with a depth of 1 and prunes all older commits, making the repository shallow.
    /// </summary>
    public void MakeShallow()
    {
        GitTestExtensions.ExecuteGitCmd($"-C {RepositoryPath} pull --depth 1");
        GitTestExtensions.ExecuteGitCmd($"-C {RepositoryPath} gc --prune=all");
    }

    public void Fetch(string remote, FetchOptions? options = null)
        => Commands.Fetch(Repository, remote, Array.Empty<string>(), options, null);
}

using GitVersion.Helpers;
using LibGit2Sharp;
using Shouldly;

namespace GitVersion.Testing;

/// <summary>
///     Fixture abstracting a git repository
/// </summary>
public abstract class RepositoryFixtureBase : IDisposable
{
    protected RepositoryFixtureBase(Func<string, Repository> repositoryBuilder)
        : this(repositoryBuilder(FileSystemHelper.Path.GetRepositoryTempPath()))
    {
    }

    protected RepositoryFixtureBase(Repository repository)
    {
        SequenceDiagram = new();
        Repository = repository.ShouldNotBeNull();
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
        var directoryPath = FileSystemHelper.Path.GetFileName(RepositoryPath);

        try
        {
            Console.WriteLine("Cleaning up repository path at {0}", directoryPath);
            FileSystemHelper.Directory.DeleteDirectory(RepositoryPath);
            Console.WriteLine("Cleaned up repository path at {0}", directoryPath);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to clean up repository path at {0}. Received exception: {1}", directoryPath, e.Message);
            // throw;
        }

        this.SequenceDiagram.End();
        Console.WriteLine("**Visualisation of test:**");
        Console.WriteLine(string.Empty);
        Console.WriteLine(this.SequenceDiagram.GetDiagram());
    }

    public void Checkout(string branch) => Commands.Checkout(Repository, branch);

    public void Remove(string branch)
    {
        Repository.Branches.Remove(branch);
        SequenceDiagram.Destroy(branch);
    }

    public static void Init(string path, string branchName = "main") => GitTestExtensions.ExecuteGitCmd($"init {path} -b {branchName}", ".");

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

    public void MergeTo(string branchName, bool removeBranchAfterMerging = false)
    {
        var mergeSource = Repository.Head.FriendlyName;
        Checkout(branchName);
        MergeNoFF(mergeSource);
        if (removeBranchAfterMerging)
        {
            Remove(mergeSource);
        }
    }

    /// <summary>
    ///     Clones the repository managed by this fixture into another LocalRepositoryFixture
    /// </summary>
    public LocalRepositoryFixture CloneRepository()
    {
        var localPath = FileSystemHelper.Path.GetRepositoryTempPath();
        Repository.Clone(RepositoryPath, localPath);
        Console.WriteLine($"Cloned repository to '{localPath}' from '{RepositoryPath}'");
        return new LocalRepositoryFixture(new Repository(localPath));
    }

    protected static Repository CreateNewRepository(string path, string branchName, int commits = 0)
    {
        Init(path, branchName);
        Console.WriteLine("Created git repository at '{0}'", path);

        var repository = new Repository(path);
        if (commits > 0)
        {
            repository.MakeCommits(commits);
        }
        return repository;
    }

    /// <summary>
    ///     Pulls with a depth of 1 and prunes all older commits, making the repository shallow.
    /// </summary>
    public void MakeShallow()
    {
        GitTestExtensions.ExecuteGitCmd($"-C {RepositoryPath} pull --depth 1", ".");
        GitTestExtensions.ExecuteGitCmd($"-C {RepositoryPath} gc --prune=all", ".");
    }

    public void Fetch(string remote, FetchOptions? options = null)
        => Commands.Fetch(Repository, remote, [], options, null);
}

using GitVersion.Extensions;
using SysPath = System.IO.Path;

namespace GitVersion.Git;

internal sealed partial class ManagedGitRepository
{
    private readonly object sessionLock = new();
    private readonly List<ManagedRepositorySession> retiredSessions = [];
    private Lazy<ManagedRepositorySession>? sessionLazy;
    private string? gitDirectory;

    private ITagCollection? tags;
    private IBranchCollection? branches;
    private ICommitCollection? commits;
    private IRemoteCollection? remotes;
    private IReferenceCollection? references;

    internal ManagedRepositorySession Session
    {
        get
        {
            var lazy = this.sessionLazy ?? throw new InvalidOperationException("Repository not initialized. Call DiscoverRepository() first.");
            return lazy.Value;
        }
    }

    internal string CliWorkingDirectory => Session.Layout.WorkingDirectory ?? Session.Layout.GitDirectory;

    public string Path => WithTrailingSeparator(Session.Layout.GitDirectory);

    public string WorkingDirectory =>
        Session.Layout.WorkingDirectory is { } workingDirectory
            ? WithTrailingSeparator(workingDirectory)
            : throw new InvalidOperationException("The repository is bare: it has no working directory.");

    public bool IsHeadDetached => Session.ReferenceStore.GetHead() is { IsSymbolic: false };
    public bool IsShallow => Session.Layout.IsShallow;
    public IBranch Head => Session.GetHead();

    public ITagCollection Tags => this.tags ??= new ManagedTagCollection(this);
    public IBranchCollection Branches => this.branches ??= new ManagedBranchCollection(this);
    public ICommitCollection Commits => this.commits ??= ManagedCommitCollection.FromHead(this);
    public IRemoteCollection Remotes => this.remotes ??= new ManagedRemoteCollection(this);
    public IReferenceCollection References => this.references ??= new ManagedReferenceCollection(this);

    public void DiscoverRepository(string? gitDirectory)
    {
        this.gitDirectory = gitDirectory;
        ResetSession();
    }

    /// <summary>
    /// Retires the current snapshot of the repository so that changes made through the git CLI
    /// (new objects, moved references, updated configuration) become visible on next access.
    /// Retired snapshots stay alive until the repository is disposed because wrappers handed
    /// out earlier may still be enumerating them.
    /// </summary>
    internal void Invalidate() => ResetSession();

    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit)
    {
        commit = commit.NotNull();
        otherCommit = otherCommit.NotNull();

        var session = Session;
        var mergeBase = session.Walker.FindMergeBase(GitObjectId.Parse(commit.Sha), GitObjectId.Parse(otherCommit.Sha));
        return mergeBase is { } id ? session.GetCommit(id) : null;
    }

    public int UncommittedChangesCount()
    {
        var session = Session;
        var headTip = Head.Tip;

        if (headTip is ManagedCommit managedTip)
        {
            return session.StatusCalculator.CountUncommittedChanges(managedTip.TreeId);
        }

        // A brand-new repository without commits: count the untracked files, mirroring the
        // libgit2 adapter's behavior (including its "the repo is really dirty" fallback).
        try
        {
            return session.StatusCalculator.CountChangesInEmptyRepository();
        }
        catch (Exception)
        {
            return int.MaxValue;
        }
    }

    public void Dispose()
    {
        lock (this.sessionLock)
        {
            if (this.sessionLazy is { IsValueCreated: true } lazy)
            {
                lazy.Value.Dispose();
            }

            this.sessionLazy = null;

            foreach (var session in this.retiredSessions)
            {
                session.Dispose();
            }

            this.retiredSessions.Clear();
        }
    }

    private void ResetSession()
    {
        lock (this.sessionLock)
        {
            if (this.sessionLazy is { IsValueCreated: true } lazy)
            {
                this.retiredSessions.Add(lazy.Value);
            }

            this.sessionLazy = new(() => new(this, CreateLayout(this.gitDirectory)));
        }
    }

    private static GitRepositoryLayout CreateLayout(string? gitDirectory)
    {
        if (gitDirectory.IsNullOrWhiteSpace())
        {
            throw new InvalidOperationException("Cannot find the .git directory");
        }

        var path = gitDirectory.TrimEnd('/', '\\');

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Cannot find the .git directory in path '{gitDirectory}'.");
        }

        if (!path.EndsWith(".git", StringComparison.Ordinal))
        {
            return GitRepositoryLayout.Discover(path);
        }

        // A dynamically cloned repository uses a working directory that is itself named
        // '.git' and contains a nested '.git' directory — treat it as a working directory,
        // the way libgit2 opens any path that contains a '.git' entry.
        var nestedDotGit = SysPath.Combine(path, ".git");

        if (Directory.Exists(nestedDotGit) || File.Exists(nestedDotGit))
        {
            return GitRepositoryLayout.Discover(path);
        }

        return SysPath.GetFileName(path) == ".git"
            ? GitRepositoryLayout.FromGitDirectory(path, SysPath.GetDirectoryName(path))
            : GitRepositoryLayout.FromGitDirectory(path, workingDirectory: null);
    }

    private static string WithTrailingSeparator(string path) =>
        path.EndsWith(SysPath.DirectorySeparatorChar) || path.EndsWith('/')
            ? path
            : path + SysPath.DirectorySeparatorChar;
}

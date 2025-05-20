using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed partial class GitRepository
{
    private Lazy<IRepository>? repositoryLazy;

    private IRepository RepositoryInstance
    {
        get
        {
            var lazy = this.repositoryLazy ?? throw new NullReferenceException("Repository not initialized. Call DiscoverRepository() first.");
            return lazy.Value;
        }
    }

    public string Path => RepositoryInstance.Info.Path;
    public string WorkingDirectory => RepositoryInstance.Info.WorkingDirectory;
    public bool IsHeadDetached => RepositoryInstance.Info.IsHeadDetached;
    public bool IsShallow => RepositoryInstance.Info.IsShallow;

    private IBranch _head = null;
    public IBranch Head {
        get {
            _head ??= new Branch(RepositoryInstance.Head);
            return _head;
        }
    }

    private ITagCollection _tags = null;
    public ITagCollection Tags {
        get {
            _tags ??= new TagCollection(RepositoryInstance.Tags);
            return _tags;
        }
    }

    private IReferenceCollection _refs = null;
    public IReferenceCollection Refs {
        get {
            _refs ??= new ReferenceCollection(RepositoryInstance.Refs);
            return _refs;
        }
    }

    private IBranchCollection _branches = null;
    public IBranchCollection Branches {
        get {
            _branches ??= new BranchCollection(RepositoryInstance.Branches);
            return _branches;
        }
    }

    private ICommitCollection _commits = null;
    public ICommitCollection Commits
    {
        get
        {
            _commits ??= new CommitCollection(RepositoryInstance.Commits);
            return _commits;
        }
    }

    public IRemoteCollection _remotes = null;
    public IRemoteCollection Remotes {
        get {
            _remotes ??= new RemoteCollection(RepositoryInstance.Network.Remotes);
            return _remotes;
        }
    }

    public void DiscoverRepository(string? gitDirectory)
    {
        if (gitDirectory?.EndsWith(".git") == false)
        {
            gitDirectory = Repository.Discover(gitDirectory);
        }
        this.repositoryLazy = new(() => new Repository(gitDirectory));
    }

    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit)
    {
        commit = commit.NotNull();
        otherCommit = otherCommit.NotNull();

        var retryAction = new RetryAction<LockedFileException, ICommit?>();
        return retryAction.Execute(() =>
        {
            var first = (Commit)commit;
            var second = (Commit)otherCommit;
            var mergeBase = RepositoryInstance.ObjectDatabase.FindMergeBase(first, second);
            return mergeBase == null ? null : new Commit(mergeBase);
        });
    }

    public int UncommittedChangesCount()
    {
        var retryAction = new RetryAction<LibGit2Sharp.LockedFileException, int>();
        return retryAction.Execute(GetUncommittedChangesCountInternal);
    }

    public void Dispose()
    {
        if (this.repositoryLazy is { IsValueCreated: true }) RepositoryInstance.Dispose();
    }

    private int GetUncommittedChangesCountInternal()
    {
        // check if we have a branch tip at all to behave properly with empty repos
        // => return that we have actually un-committed changes because we are apparently
        // running GitVersion on something which lives inside this brand-new repo _/\Ö/\_
        if (RepositoryInstance.Head?.Tip == null || RepositoryInstance.Diff == null)
        {
            // this is a somewhat cumbersome way of figuring out the number of changes in the repo
            // which is more expensive than to use the Diff as it gathers more info, but
            // we can't use the other method when we are dealing with a new/empty repo
            try
            {
                var status = RepositoryInstance.RetrieveStatus();
                return status.Untracked.Count() + status.Staged.Count();
            }
            catch (Exception)
            {
                return int.MaxValue; // this should be somewhat puzzling to see,
                // so we may have reached our goal to show that
                // that repo is really "Dirty"...
            }
        }

        // gets all changes of the last commit vs Staging area and WT
        var changes = RepositoryInstance.Diff.Compare<TreeChanges>(RepositoryInstance.Head.Tip.Tree,
            DiffTargets.Index | DiffTargets.WorkingDirectory);

        return changes.Count;
    }
}

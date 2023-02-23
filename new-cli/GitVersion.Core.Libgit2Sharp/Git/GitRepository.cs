using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion;

public sealed class GitRepository : IGitRepository
{
    private Lazy<IRepository>? repositoryLazy;
    private IRepository Instance
    {
        get
        {
            var lazy = this.repositoryLazy ?? throw new NullReferenceException("Repository not initialized. Call Discover() first.");
            return lazy.Value;
        }
    }

    public void Discover(string gitDirectory)
    {
        gitDirectory = Repository.Discover(gitDirectory);
        this.repositoryLazy = new Lazy<IRepository>(() => new Repository(gitDirectory));
    }

    public void Dispose()
    {
        var lazy = this.repositoryLazy;
        if (lazy is { IsValueCreated: true }) Instance.Dispose();
    }

    public string Path => Instance.Info.Path;
    public string WorkingDirectory => Instance.Info.WorkingDirectory;
    public bool IsHeadDetached => Instance.Info.IsHeadDetached;

    public IBranch Head => new Branch(Instance.Head);
    public ITagCollection Tags => new TagCollection(Instance.Tags);
    public IReferenceCollection Refs => new ReferenceCollection(Instance.Refs);
    public IBranchCollection Branches => new BranchCollection(Instance.Branches);
    public ICommitCollection Commits => new CommitCollection(Instance.Commits);
    public IRemoteCollection Remotes => new RemoteCollection(Instance.Network.Remotes);

    public ICommit? FindMergeBase(ICommit commit, ICommit otherCommit)
    {
        _ = commit.NotNull();
        _ = otherCommit.NotNull();

        var retryAction = new RetryAction<LockedFileException, ICommit?>();
        return retryAction.Execute(() =>
        {
            var mergeBase = Instance.ObjectDatabase.FindMergeBase((Commit)commit, (Commit)otherCommit);
            return mergeBase == null ? null : new Commit(mergeBase);
        });
    }
    public int GetNumberOfUncommittedChanges()
    {
        var retryAction = new RetryAction<LibGit2Sharp.LockedFileException, int>();
        return retryAction.Execute(GetNumberOfUncommittedChangesInternal);
    }
    private int GetNumberOfUncommittedChangesInternal()
    {
        // check if we have a branch tip at all to behave properly with empty repos
        // => return that we have actually un-committed changes because we are apparently
        // running GitVersion on something which lives inside this brand new repo _/\Ö/\_
        if (Instance.Head?.Tip == null || Instance.Diff == null)
        {
            // this is a somewhat cumbersome way of figuring out the number of changes in the repo
            // which is more expensive than to use the Diff as it gathers more info, but
            // we can't use the other method when we are dealing with a new/empty repo
            try
            {
                var status = Instance.RetrieveStatus();
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
        var changes = Instance.Diff.Compare<TreeChanges>(Instance.Head.Tip.Tree,
            DiffTargets.Index | DiffTargets.WorkingDirectory);

        return changes.Count;
    }
}

using System.Collections.Concurrent;
using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed partial class GitRepository
{
    private Lazy<IRepository>? repositoryLazy;
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>?> patchPathsCache = new();

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
    public IBranch Head => new Branch(RepositoryInstance.Head);

    public ITagCollection Tags => new TagCollection(RepositoryInstance.Tags);
    public IReferenceCollection Refs => new ReferenceCollection(RepositoryInstance.Refs);
    public IBranchCollection Branches => new BranchCollection(RepositoryInstance.Branches);
    public ICommitCollection Commits => new CommitCollection(RepositoryInstance.Commits);
    public IRemoteCollection Remotes => new RemoteCollection(RepositoryInstance.Network.Remotes);

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

    public IReadOnlyList<string>? FindPatchPaths(ICommit commit, string? tagPrefix)
    {
        ArgumentNullException.ThrowIfNull(commit);

        return patchPathsCache.GetOrAdd(commit.Sha, commitSha =>
        {
            if (PatchPathsNeedsToBeDetermined(commitSha, tagPrefix))
            {
                var innerCommit = this.RepositoryInstance.Commits.First(c => c.Sha == commitSha);
                Tree commitTree = innerCommit.Tree; // Main Tree
                Tree? parentCommitTree = innerCommit.Parents.FirstOrDefault()?.Tree; // Secondary Tree
                Patch patch = this.RepositoryInstance.Diff.Compare<Patch>(parentCommitTree, commitTree); // Difference
                return patch.Select(element => element.Path).ToList();
            }
            return null;
        });
    }

    private bool PatchPathsNeedsToBeDetermined(string commitSha, string? tagPrefix)
    {
        if (!string.IsNullOrEmpty(tagPrefix))
        {
            foreach (var tag in this.RepositoryInstance.Tags.Where(element => element.Target.Sha == commitSha))
            {
                if (tag.FriendlyName.StartsWith(tagPrefix, StringComparison.InvariantCulture))
                {
                    return false;
                }
            }
        }
        return true;
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

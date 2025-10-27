using System.Collections.Concurrent;
using GitVersion.Extensions;
using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion.Git;

internal sealed partial class GitRepository
{
    private Lazy<IRepository>? repositoryLazy;

    private readonly ConcurrentDictionary<string, Branch> cachedBranches = new();
    private readonly ConcurrentDictionary<string, Commit> cachedCommits = new();
    private readonly ConcurrentDictionary<string, Tag> cachedTags = new();
    private readonly ConcurrentDictionary<string, Remote> cachedRemotes = new();
    private readonly ConcurrentDictionary<string, Reference> cachedReferences = new();
    private readonly ConcurrentDictionary<string, RefSpec> cachedRefSpecs = new();

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
    public IBranch Head => GetOrCreate(RepositoryInstance.Head, RepositoryInstance.Diff);

    public ITagCollection Tags => new TagCollection(RepositoryInstance.Tags, RepositoryInstance.Diff, this);
    public IBranchCollection Branches => new BranchCollection(RepositoryInstance.Branches, RepositoryInstance.Diff, this);
    public ICommitCollection Commits => new CommitCollection(RepositoryInstance.Commits, RepositoryInstance.Diff, this);
    public IRemoteCollection Remotes => new RemoteCollection(RepositoryInstance.Network.Remotes, this);
    public IReferenceCollection References => new ReferenceCollection(RepositoryInstance.Refs, this);

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
            return mergeBase == null ? null : GetOrCreate(mergeBase, RepositoryInstance.Diff);
        });
    }

    public int UncommittedChangesCount()
    {
        var retryAction = new RetryAction<LibGit2Sharp.LockedFileException, int>();
        return retryAction.Execute(GetUncommittedChangesCountInternal);
    }

    public Branch GetOrCreate(LibGit2Sharp.Branch innerBranch, Diff repoDiff)
    {
        var cacheKey = innerBranch.Tip is null
            ? $"{innerBranch.RemoteName}/{innerBranch.CanonicalName}"
            : $"{innerBranch.RemoteName}/{innerBranch.CanonicalName}@{innerBranch.Tip.Sha}";
        return cachedBranches.GetOrAdd(cacheKey, _ => new Branch(innerBranch, repoDiff, this));
    }

    public Commit GetOrCreate(LibGit2Sharp.Commit innerCommit, Diff repoDiff)
        => cachedCommits.GetOrAdd(innerCommit.Sha, _ => new Commit(innerCommit, repoDiff, this));

    public Tag GetOrCreate(LibGit2Sharp.Tag innerTag, Diff repoDiff)
        => cachedTags.GetOrAdd(innerTag.CanonicalName, _ => new Tag(innerTag, repoDiff, this));

    public Remote GetOrCreate(LibGit2Sharp.Remote innerRemote)
        => cachedRemotes.GetOrAdd(innerRemote.Name, _ => new Remote(innerRemote, this));

    public Reference GetOrCreate(LibGit2Sharp.Reference innerReference)
        => cachedReferences.GetOrAdd(innerReference.CanonicalName, _ => new Reference(innerReference));

    public RefSpec GetOrCreate(LibGit2Sharp.RefSpec innerRefSpec)
        => cachedRefSpecs.GetOrAdd(innerRefSpec.Specification, _ => new RefSpec(innerRefSpec));

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
        var changes = RepositoryInstance.Diff.Compare<LibGit2Sharp.TreeChanges>(RepositoryInstance.Head.Tip.Tree,
            DiffTargets.Index | DiffTargets.WorkingDirectory);

        return changes.Count;
    }
}

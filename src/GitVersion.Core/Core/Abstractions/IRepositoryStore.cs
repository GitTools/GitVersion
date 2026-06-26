using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion;

/// <summary>Provides high-level read access to a Git repository, exposing the objects and queries needed for version calculation.</summary>
public interface IRepositoryStore
{
    /// <summary>Gets the number of files that have been modified but not yet committed.</summary>
    int UncommittedChangesCount { get; }

    /// <summary>Gets the currently checked-out branch.</summary>
    IBranch Head { get; }

    /// <summary>Gets the collection of all branches in the repository.</summary>
    IBranchCollection Branches { get; }

    /// <summary>Gets the collection of all tags in the repository.</summary>
    ITagCollection Tags { get; }

    /// <summary>
    /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch);

    /// <summary>Finds the best common ancestor between <paramref name="commit"/> and <paramref name="mainlineTip"/>.</summary>
    ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip);

    /// <summary>Returns the commit that should be treated as the current HEAD for version calculation, applying ignore rules.</summary>
    ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId, IIgnoreConfiguration ignore);

    /// <summary>Returns the commit that represents a forward merge from <paramref name="commitToFindCommonBase"/> relative to <paramref name="findMergeBase"/>.</summary>
    ICommit? GetForwardMerge(ICommit? commitToFindCommonBase, ICommit? findMergeBase);

    /// <summary>Returns the commits reachable between <paramref name="baseVersionSource"/> and <paramref name="currentCommit"/>, respecting ignore rules.</summary>
    IReadOnlyList<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore);

    /// <summary>Returns all commits reachable from the HEAD commit, respecting ignore rules.</summary>
    IReadOnlyList<ICommit> GetCommitsReacheableFromHead(ICommit? headCommit, IIgnoreConfiguration ignore);

    /// <summary>Returns all commits reachable from <paramref name="commit"/> that are also on <paramref name="branch"/>.</summary>
    IReadOnlyList<ICommit> GetCommitsReacheableFrom(ICommit commit, IBranch branch);

    /// <summary>Resolves and returns the branch that matches <paramref name="targetBranchName"/>, creating a local branch if necessary.</summary>
    IBranch GetTargetBranch(string? targetBranchName);

    /// <summary>Finds and returns the branch with the given <paramref name="branchName"/>, or <see langword="null"/> if not found.</summary>
    IBranch? FindBranch(ReferenceName branchName);

    /// <summary>Returns all branches except those in <paramref name="branchesToExclude"/>.</summary>
    IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);

    /// <summary>Returns branches that contain the given <paramref name="commit"/> in their history.</summary>
    IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false);

    /// <summary>Returns the commits and their originating branches that branched from <paramref name="branch"/>, excluding <paramref name="excludedBranches"/>.</summary>
    IEnumerable<BranchCommit> FindCommitBranchesBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    /// <summary>Returns the branches that <paramref name="branch"/> was directly branched from, excluding <paramref name="excludedBranches"/>.</summary>
    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    /// <summary>Returns the branches that <paramref name="branch"/> was directly branched from, excluding the given collection of branches.</summary>
    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    /// <summary>Returns <see langword="true"/> if <paramref name="baseVersionSource"/> is an ancestor of <paramref name="firstMatchingCommit"/> on <paramref name="branch"/>.</summary>
    bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit);
}

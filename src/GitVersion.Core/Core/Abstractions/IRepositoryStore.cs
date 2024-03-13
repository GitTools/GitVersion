using GitVersion.Configuration;

namespace GitVersion.Common;

public interface IRepositoryStore
{
    /// <summary>
    /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch);

    ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip);

    ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId, IIgnoreConfiguration ignore);

    IReadOnlyList<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore);

    IBranch GetTargetBranch(string? targetBranchName);
    IBranch? FindBranch(ReferenceName branchName);
    IBranch? FindBranch(string branchName);
    IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, IBranchConfiguration>> releaseBranchConfig);
    IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);
    IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false);

    /// <summary>
    /// Find the commit where the given branch was branched from another branch.
    /// If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    BranchCommit FindCommitBranchWasBranchedFrom(IBranch? branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit);

    int GetNumberOfUncommittedChanges();
}

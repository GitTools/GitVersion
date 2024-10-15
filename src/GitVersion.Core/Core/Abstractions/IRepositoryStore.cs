using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.Common;

public interface IRepositoryStore
{
    int UncommittedChangesCount { get; }
    IBranch Head { get; }
    IBranchCollection Branches { get; }
    ITagCollection Tags { get; }

    /// <summary>
    /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch);

    ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip);

    ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId, IIgnoreConfiguration ignore);
    ICommit? GetForwardMerge(ICommit? commitToFindCommonBase, ICommit? findMergeBase);

    IReadOnlyList<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore);
    IReadOnlyList<ICommit> GetCommitsReacheableFromHead(ICommit? headCommit, IIgnoreConfiguration ignore);
    IReadOnlyList<ICommit> GetCommitsReacheableFrom(IGitObject commit, IBranch branch);

    IBranch GetTargetBranch(string? targetBranchName);
    IBranch? FindBranch(ReferenceName branchName);

    IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);
    IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false);

    /// <summary>
    /// Find the commit where the given branch was branched from another branch.
    /// If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    BranchCommit FindCommitBranchBranchedFrom(IBranch? branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit);
}

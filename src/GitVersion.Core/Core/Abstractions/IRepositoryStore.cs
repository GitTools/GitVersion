using GitVersion.Configuration;

namespace GitVersion.Common;

public interface IRepositoryStore
{
    /// <summary>
    /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch);

    ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip);
    ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId);
    IEnumerable<ICommit> GetMainlineCommitLog(ICommit? baseVersionSource, ICommit? mainlineTip);
    IEnumerable<ICommit> GetMergeBaseCommits(ICommit? mergeCommit, ICommit? mergedHead, ICommit? findMergeBase);
    IEnumerable<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit? currentCommit);

    IBranch GetTargetBranch(string? targetBranchName);
    IBranch? FindBranch(string? branchName);
    IBranch? FindMainBranch(GitVersionConfiguration configuration);
    IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfiguration>> releaseBranchConfig);
    IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);
    IEnumerable<IBranch> GetBranchesContainingCommit(ICommit? commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false);

    IDictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, GitVersionConfiguration configuration);

    /// <summary>
    /// Find the commit where the given branch was branched from another branch.
    /// If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    BranchCommit FindCommitBranchWasBranchedFrom(IBranch? branch, GitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, GitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, GitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, GitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, GitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    SemanticVersion? GetCurrentCommitTaggedVersion(ICommit? commit, GitVersionConfiguration configuration);

    IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string? tagPrefixRegex);
    IEnumerable<(ITag Tag, SemanticVersion Semver, ICommit Commit)> GetValidVersionTags(string? tagPrefixRegex, DateTimeOffset? olderThan = null);

    bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit);

    int GetNumberOfUncommittedChanges();
}

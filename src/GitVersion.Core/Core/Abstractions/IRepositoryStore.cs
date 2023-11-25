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
    IBranch? FindBranch(ReferenceName branchName);
    IBranch? FindBranch(string branchName);
    IBranch? FindMainBranch(IGitVersionConfiguration configuration);
    IEnumerable<IBranch> FindMainlineBranches(IGitVersionConfiguration configuration);
    IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, IBranchConfiguration>> releaseBranchConfig);
    IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);
    IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false);

    IDictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, IGitVersionConfiguration configuration);

    /// <summary>
    /// Find the commit where the given branch was branched from another branch.
    /// If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    BranchCommit FindCommitBranchWasBranchedFrom(IBranch? branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<BranchCommit> FindCommitBranchesWasBranchedFrom(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, params IBranch[] excludedBranches);

    IEnumerable<IBranch> GetSourceBranches(IBranch branch, IGitVersionConfiguration configuration, IEnumerable<IBranch> excludedBranches);

    SemanticVersion? GetCurrentCommitTaggedVersion(ICommit? commit, string? tagPrefix, SemanticVersionFormat format, bool handleDetachedBranch);

    IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string? tagPrefix, SemanticVersionFormat format);

    IReadOnlyList<SemanticVersionWithTag> GetTaggedSemanticVersions(string? tagPrefix, SemanticVersionFormat format);

    IReadOnlyList<SemanticVersionWithTag> GetTaggedSemanticVersionsOnBranch(IBranch branch, string? tagPrefix, SemanticVersionFormat format);

    bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit);

    int GetNumberOfUncommittedChanges();

    IEnumerable<SemanticVersionWithTag> GetSemanticVersions(
        IGitVersionConfiguration configuration, IBranch currentBranch, ICommit currentCommit,
        bool trackMergeTarget, bool tracksReleaseBranches
    );
}

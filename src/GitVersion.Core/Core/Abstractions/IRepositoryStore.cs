using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Common;

public interface IRepositoryStore
{
    /// <summary>
    /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
    /// </summary>
    ICommit? FindMergeBase(IBranch? branch, IBranch? otherBranch);
    ICommit? FindMergeBase(ICommit commit, ICommit mainlineTip);
    ICommit? GetCurrentCommit(IBranch currentBranch, string? commitId);
    ICommit GetBaseVersionSource(ICommit currentBranchTip);
    IEnumerable<ICommit> GetMainlineCommitLog(ICommit? baseVersionSource, ICommit? mainlineTip);
    IEnumerable<ICommit> GetMergeBaseCommits(ICommit? mergeCommit, ICommit? mergedHead, ICommit? findMergeBase);
    IEnumerable<ICommit> GetCommitLog(ICommit? baseVersionSource, ICommit? currentCommit);

    IBranch GetTargetBranch(string? targetBranchName);
    IBranch? FindBranch(string? branchName);
    IBranch? FindMainBranch(Config configuration);
    IBranch? GetChosenBranch(Config configuration);
    IEnumerable<IBranch> GetBranchesForCommit(ICommit commit);
    IEnumerable<IBranch> GetExcludedInheritBranches(Config configuration);
    IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig>> releaseBranchConfig);
    IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude, bool excludeRemotes = false);
    IEnumerable<IBranch> GetBranchesContainingCommit(ICommit? commit, IEnumerable<IBranch>? branches = null, bool onlyTrackedBranches = false);
    IDictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, Config configuration, IEnumerable<KeyValuePair<string, BranchConfig>>? mainlineBranchConfigs);

    /// <summary>
    /// Find the commit where the given branch was branched from another branch.
    /// If there are multiple such commits and branches, tries to guess based on commit histories.
    /// </summary>
    BranchCommit FindCommitBranchWasBranchedFrom(IBranch? branch, Config configuration, params IBranch[] excludedBranches);

    SemanticVersion GetCurrentCommitTaggedVersion(ICommit? commit, EffectiveConfiguration config);
    SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context);
    IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string? tagPrefixRegex);
    IEnumerable<(ITag Tag, SemanticVersion Semver, ICommit Commit)> GetValidVersionTags(string? tagPrefixRegex, DateTimeOffset? olderThan = null);

    bool IsCommitOnBranch(ICommit? baseVersionSource, IBranch branch, ICommit firstMatchingCommit);
    VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context);

    int GetNumberOfUncommittedChanges();
}

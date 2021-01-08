using System;
using System.Collections.Generic;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Common
{
    public interface IRepositoryMetadataProvider
    {
        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        Commit FindMergeBase(IBranch branch, IBranch otherBranch);
        Commit FindMergeBase(Commit commit, Commit mainlineTip);
        Commit GetCurrentCommit(IBranch currentBranch, string commitId);
        Commit GetBaseVersionSource(Commit currentBranchTip);
        List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip);
        IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase);

        IBranch GetTargetBranch(string targetBranch);
        IBranch FindBranch(string branchName);
        IBranch GetChosenBranch(Config configuration);
        List<IBranch> GetBranchesForCommit(Commit commit);
        List<IBranch> GetExcludedInheritBranches(Config configuration);
        IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig>> releaseBranchConfig);
        IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);
        IEnumerable<IBranch> GetBranchesContainingCommit(Commit commit, IEnumerable<IBranch> branches = null, bool onlyTrackedBranches = false);
        Dictionary<string, List<IBranch>> GetMainlineBranches(Commit commit, IEnumerable<KeyValuePair<string, BranchConfig>> mainlineBranchConfigs);

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, tries to guess based on commit histories.
        /// </summary>
        BranchCommit FindCommitBranchWasBranchedFrom(IBranch branch, Config configuration, params IBranch[] excludedBranches);

        SemanticVersion GetCurrentCommitTaggedVersion(Commit commit, EffectiveConfiguration config);
        SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context);
        IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string tagPrefixRegex);
        IEnumerable<Tuple<ITag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null);

        CommitCollection GetCommitLog(Commit baseVersionSource, Commit currentCommit);
        bool GetMatchingCommitBranch(Commit baseVersionSource, IBranch branch, Commit firstMatchingCommit);
        string ShortenObjectId(Commit commit);
        VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context);

        int GetNumberOfUncommittedChanges();
    }
}

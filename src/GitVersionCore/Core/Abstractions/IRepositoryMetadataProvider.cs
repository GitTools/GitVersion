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
        ICommit FindMergeBase(IBranch branch, IBranch otherBranch);
        ICommit FindMergeBase(ICommit commit, ICommit mainlineTip);
        ICommit GetCurrentCommit(IBranch currentBranch, string commitId);
        ICommit GetBaseVersionSource(ICommit currentBranchTip);
        IEnumerable<ICommit> GetMainlineCommitLog(ICommit baseVersionSource, ICommit mainlineTip);
        IEnumerable<ICommit> GetMergeBaseCommits(ICommit mergeCommit, ICommit mergedHead, ICommit findMergeBase);

        IBranch GetTargetBranch(string targetBranch);
        IBranch FindBranch(string branchName);
        IBranch GetChosenBranch(Config configuration);
        List<IBranch> GetBranchesForCommit(ICommit commit);
        List<IBranch> GetExcludedInheritBranches(Config configuration);
        IEnumerable<IBranch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig>> releaseBranchConfig);
        IEnumerable<IBranch> ExcludingBranches(IEnumerable<IBranch> branchesToExclude);
        IEnumerable<IBranch> GetBranchesContainingCommit(ICommit commit, IEnumerable<IBranch> branches = null, bool onlyTrackedBranches = false);
        Dictionary<string, List<IBranch>> GetMainlineBranches(ICommit commit, IEnumerable<KeyValuePair<string, BranchConfig>> mainlineBranchConfigs);

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, tries to guess based on commit histories.
        /// </summary>
        BranchCommit FindCommitBranchWasBranchedFrom(IBranch branch, Config configuration, params IBranch[] excludedBranches);

        SemanticVersion GetCurrentCommitTaggedVersion(ICommit commit, EffectiveConfiguration config);
        SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context);
        IEnumerable<SemanticVersion> GetVersionTagsOnBranch(IBranch branch, string tagPrefixRegex);
        IEnumerable<Tuple<ITag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null);

        IEnumerable<ICommit> GetCommitLog(ICommit baseVersionSource, ICommit currentCommit);
        bool GetMatchingCommitBranch(ICommit baseVersionSource, IBranch branch, ICommit firstMatchingCommit);
        string ShortenObjectId(ICommit commit);
        VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context);

        int GetNumberOfUncommittedChanges();
    }
}

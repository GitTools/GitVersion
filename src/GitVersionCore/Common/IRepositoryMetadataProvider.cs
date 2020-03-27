using System;
using System.Collections.Generic;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Common
{
    public interface IRepositoryMetadataProvider
    {
        IEnumerable<Tuple<Tag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null);
        IEnumerable<SemanticVersion> GetVersionTagsOnBranch(Branch branch, string tagPrefixRegex);
        IEnumerable<Branch> GetBranchesContainingCommit(Commit commit, IEnumerable<Branch> branches = null, bool onlyTrackedBranches = false);

        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        Commit FindMergeBase(Branch branch, Branch otherBranch);
        Commit FindMergeBase(Commit commit, Commit mainlineTip);

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, tries to guess based on commit histories.
        /// </summary>
        BranchCommit FindCommitBranchWasBranchedFrom(Branch branch, Config configuration, params Branch[] excludedBranches);
        Branch GetTargetBranch(string targetBranch);
        Commit GetCurrentCommit(Branch currentBranch, string commitId);
        SemanticVersion GetCurrentCommitTaggedVersion(GitObject commit, EffectiveConfiguration config);
        ICommitLog GetCommitLog(Commit baseVersionSource, Commit currentCommit);
        bool GetMatchingCommitBranch(Commit baseVersionSource, Branch branch, Commit firstMatchingCommit);
        List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip);
        IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase);
        string ShortenObjectId(GitObject commit);
        Dictionary<string, List<Branch>> GetMainlineBranches(Commit commit, IEnumerable<KeyValuePair<string, BranchConfig>> mainlineBranchConfigs);
        Commit GetBaseVersionSource(Commit currentBranchTip);
        IEnumerable<Branch> GetReleaseBranches(IReadOnlyCollection<KeyValuePair<string, BranchConfig>> releaseBranchConfig);
        Branch FindBranch(string branchName);
        SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context);
        VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context);
        List<Branch> GetBranchesForCommit(GitObject commit);
        IEnumerable<Branch> ExcludingBranches(IEnumerable<Branch> branchesToExclude);
        List<Branch> GetExcludedInheritBranches(Config configuration);
        Branch GetChosenBranch(Config configuration);
    }
}

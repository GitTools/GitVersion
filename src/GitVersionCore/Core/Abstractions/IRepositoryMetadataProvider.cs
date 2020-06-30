using System;
using System.Collections.Generic;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersion.Common
{
    public interface IRepositoryMetadataProvider
    {
        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        Commit FindMergeBase(Branch branch, Branch otherBranch);
        Commit FindMergeBase(Commit commit, Commit mainlineTip);
        Commit GetCurrentCommit(Branch currentBranch, string commitId);
        Commit GetBaseVersionSource(Commit currentBranchTip);
        List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip);
        IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase);

        Branch GetTargetBranch(string targetBranch);
        Branch FindBranch(string branchName);
        Branch GetChosenBranch(Config configuration);
        List<Branch> GetBranchesForCommit(GitObject commit);
        List<Branch> GetExcludedInheritBranches(Config configuration);
        IEnumerable<Branch> GetReleaseBranches(IEnumerable<KeyValuePair<string, BranchConfig>> releaseBranchConfig);
        IEnumerable<Branch> ExcludingBranches(IEnumerable<Branch> branchesToExclude);
        IEnumerable<Branch> GetBranchesContainingCommit(Commit commit, IEnumerable<Branch> branches = null, bool onlyTrackedBranches = false);
        Dictionary<string, List<Branch>> GetMainlineBranches(Commit commit, IEnumerable<KeyValuePair<string, BranchConfig>> mainlineBranchConfigs);

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, tries to guess based on commit histories.
        /// </summary>
        BranchCommit FindCommitBranchWasBranchedFrom(Branch branch, Config configuration, params Branch[] excludedBranches);

        SemanticVersion GetCurrentCommitTaggedVersion(GitObject commit, EffectiveConfiguration config);
        SemanticVersion MaybeIncrement(BaseVersion baseVersion, GitVersionContext context);
        IEnumerable<SemanticVersion> GetVersionTagsOnBranch(Branch branch, string tagPrefixRegex);
        IEnumerable<Tuple<Tag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null);

        ICommitLog GetCommitLog(Commit baseVersionSource, Commit currentCommit);
        bool GetMatchingCommitBranch(Commit baseVersionSource, Branch branch, Commit firstMatchingCommit);
        string ShortenObjectId(GitObject commit);
        VersionField? DetermineIncrementedField(BaseVersion baseVersion, GitVersionContext context);
    }
}

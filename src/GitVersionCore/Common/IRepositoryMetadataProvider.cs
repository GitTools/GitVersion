using System;
using System.Collections.Generic;
using GitVersion.Model.Configuration;
using LibGit2Sharp;

namespace GitVersion.Common
{
    public interface IRepositoryMetadataProvider
    {
        IEnumerable<Tuple<Tag, SemanticVersion>> GetValidVersionTags(string tagPrefixRegex, DateTimeOffset? olderThan = null);
        IEnumerable<SemanticVersion> GetVersionTagsOnBranch(Branch branch, string tagPrefixRegex);
        IEnumerable<Branch> GetBranchesContainingCommit(Commit commit, IEnumerable<Branch> branches, bool onlyTrackedBranches);

        /// <summary>
        /// Find the merge base of the two branches, i.e. the best common ancestor of the two branches' tips.
        /// </summary>
        Commit FindMergeBase(Branch branch, Branch otherBranch);

        /// <summary>
        /// Find the commit where the given branch was branched from another branch.
        /// If there are multiple such commits and branches, tries to guess based on commit histories.
        /// </summary>
        BranchCommit FindCommitBranchWasBranchedFrom(Branch branch, Config configuration, params Branch[] excludedBranches);
    }
}

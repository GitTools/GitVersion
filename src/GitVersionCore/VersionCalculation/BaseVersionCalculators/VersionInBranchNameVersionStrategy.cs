using System;
using System.Collections.Generic;
using LibGit2Sharp;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    /// <summary>
    /// Version is extracted from the name of the branch.
    /// BaseVersionSource is the commit where the branch was branched from its parent.
    /// Does not increment.
    /// </summary>
    public class VersionInBranchNameVersionStrategy : IVersionStrategy
    {
        public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var currentBranch = context.CurrentBranch;
            var tagPrefixRegex = context.Configuration.GitTagPrefix;
            return GetVersions(context, tagPrefixRegex, currentBranch);
        }

        public IEnumerable<BaseVersion> GetVersions(GitVersionContext context, string tagPrefixRegex, Branch currentBranch)
        {
            if (!context.FullConfiguration.IsReleaseBranch(currentBranch.NameWithoutOrigin()))
            {
                yield break;
            }

            var branchName = currentBranch.FriendlyName;
            var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
            if (versionInBranch != null)
            {
                var commitBranchWasBranchedFrom = context.RepositoryMetadataProvider.FindCommitBranchWasBranchedFrom(currentBranch);
                var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                yield return new BaseVersion(context, "Version in branch name", false, versionInBranch.Item2, commitBranchWasBranchedFrom.Commit, branchNameOverride);
            }
        }

        private Tuple<string, SemanticVersion> GetVersionInBranch(string branchName, string tagPrefixRegex)
        {
            var branchParts = branchName.Split('/', '-');
            foreach (var part in branchParts)
            {
                if (SemanticVersion.TryParse(part, tagPrefixRegex, out var semanticVersion))
                {
                    return Tuple.Create(part, semanticVersion);
                }
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using LibGit2Sharp;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is extracted from the name of the branch.
    /// BaseVersionSource is the commit where the branch was branched from its parent.
    /// Does not increment.
    /// </summary>
    public class VersionInBranchNameVersionStrategy : VersionStrategyBase
    {
        private IRepositoryMetadataProvider repositoryMetadataProvider;

        public VersionInBranchNameVersionStrategy(IRepositoryMetadataProvider repositoryMetadataProvider, Lazy<GitVersionContext> versionContext) : base(versionContext)
        {
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            var currentBranch = Context.CurrentBranch;
            var tagPrefixRegex = Context.Configuration.GitTagPrefix;
            return GetVersions(tagPrefixRegex, currentBranch);
        }

        internal IEnumerable<BaseVersion> GetVersions(string tagPrefixRegex, Branch currentBranch)
        {
            if (!Context.FullConfiguration.IsReleaseBranch(currentBranch.NameWithoutOrigin()))
            {
                yield break;
            }

            var branchName = currentBranch.FriendlyName;
            var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
            if (versionInBranch != null)
            {
                var commitBranchWasBranchedFrom = repositoryMetadataProvider.FindCommitBranchWasBranchedFrom(currentBranch, Context.FullConfiguration);
                var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                yield return new BaseVersion("Version in branch name", false, versionInBranch.Item2, commitBranchWasBranchedFrom.Commit, branchNameOverride);
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

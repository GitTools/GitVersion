namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;
    using LibGit2Sharp;

    public class VersionInBranchBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var currentBranch = context.CurrentBranch;
            var tagPrefixRegex = context.Configuration.GitTagPrefix;
            var repository = context.Repository;
            return GetVersions(tagPrefixRegex, currentBranch, repository);
        }

        public IEnumerable<BaseVersion> GetVersions(string tagPrefixRegex, Branch currentBranch, IRepository repository)
        {
            var branchName = currentBranch.FriendlyName;
            var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
            if (versionInBranch != null)
            {
                var commitBranchWasBranchedFrom = currentBranch.FindCommitBranchWasBranchedFrom(repository);
                var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                yield return new BaseVersion("Version in branch name", false, versionInBranch.Item2, commitBranchWasBranchedFrom, branchNameOverride);
            }
        }

        Tuple<string, SemanticVersion> GetVersionInBranch(string branchName, string tagPrefixRegex)
        {
            var branchParts = branchName.Split('/', '-');
            foreach (var part in branchParts)
            {
                SemanticVersion semanticVersion;
                if (SemanticVersion.TryParse(part, tagPrefixRegex, out semanticVersion))
                {
                    return Tuple.Create(part, semanticVersion);
                }
            }

            return null;
        }
    }
}
namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Version is extracted from the name of the branch.
    /// BaseVersionSource is the commit where the branch was branched from its parent.
    /// Does not increment.
    /// </summary>
    public class VersionInBranchBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            return GetBranchBaseVersions(context);
        }

        public IEnumerable<BaseVersion> GetBranchBaseVersions(GitVersionContext context)
        {
            foreach (var config in context.Configurations)
            {
                var branchName = config.CurrentBranchInfo.Branch.FriendlyName;
                var tagPrefixRegex = config.GitTagPrefix;
                var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
                if (versionInBranch != null)
                {
                    var parentBranchCommit = config.CurrentBranchInfo.LastCommit;
                    var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                    yield return new BaseVersion("Version in branch name", false, versionInBranch.Item2, parentBranchCommit, branchNameOverride);
                }
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
namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;
    using LibGit2Sharp;
    using GitVersion.GitRepoInformation;

    /// <summary>
    /// Version is extracted from the name of the branch.
    /// BaseVersionSource is the commit where the branch was branched from its parent.
    /// Does not increment.
    /// </summary>
    public class VersionInBranchNameBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var currentBranch = context.RepositoryMetadata.CurrentBranch;
            var tagPrefixRegex = context.Configuration.GitTagPrefix;
            var repository = context.Repository;
            return GetVersions(context, tagPrefixRegex, currentBranch, repository);
        }

        public IEnumerable<BaseVersion> GetVersions(GitVersionContext context, string tagPrefixRegex, MBranch currentBranch, IRepository repository)
        {
            var branchName = currentBranch.Name;
            var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
            if (versionInBranch != null)
            {
                var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                var source = new BaseVersionSource(currentBranch.Parent.MergeBase, $"Version in branch name with origin commit of {currentBranch.Parent.MergeBase.Sha}");
                yield return new BaseVersion(context, false, versionInBranch.Item2, source, branchNameOverride);
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
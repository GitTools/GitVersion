namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;

    public class VersionInBranchBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var versionInBranch = GetVersionInBranch(context);
            if (versionInBranch != null)
            {
                var commitBranchWasBranchedFrom = context.CurrentBranch.FindCommitBranchWasBranchedFrom(context.Repository);
                var branchNameOverride = context.CurrentBranch.FriendlyName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                yield return new BaseVersion("Version in branch name", false, versionInBranch.Item2, commitBranchWasBranchedFrom, branchNameOverride);
            }
        }

        Tuple<string, SemanticVersion> GetVersionInBranch(GitVersionContext context)
        {
            var branchParts = context.CurrentBranch.FriendlyName.Split('/', '-');
            foreach (var part in branchParts)
            {
                SemanticVersion semanticVersion;
                if (SemanticVersion.TryParse(part, context.Configuration.GitTagPrefix, out semanticVersion))
                {
                    return Tuple.Create(part, semanticVersion);
                }
            }

            return null;
        }
    }
}
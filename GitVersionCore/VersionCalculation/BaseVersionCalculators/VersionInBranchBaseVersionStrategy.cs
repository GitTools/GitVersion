namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;

    public class VersionInBranchBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            var versionInBranch = GetVersionInBranch(context);
            if (versionInBranch != null)
            {
                var commitBranchWasBranchedFrom = context.CurrentBranch.FindCommitBranchWasBranchedFrom(context.Repository);
                var branchNameOverride = context.CurrentBranch.Name.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                return new BaseVersion("Version in branch name", false, true, versionInBranch.Item2, commitBranchWasBranchedFrom, branchNameOverride);
            }

            return null;
        }

        Tuple<string, SemanticVersion> GetVersionInBranch(GitVersionContext context)
        {
            var branchParts = context.CurrentBranch.Name.Split('/', '-');
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
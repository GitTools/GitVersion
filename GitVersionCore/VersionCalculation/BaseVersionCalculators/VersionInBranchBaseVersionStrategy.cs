namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Linq;

    public class VersionInBranchBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            var versionInBranch = GetVersionInBranch(context);
            if (versionInBranch != null)
            {
                var firstCommitOfBranch = context.CurrentBranch.Commits.Last();
                return new BaseVersion(false, versionInBranch.Item2, firstCommitOfBranch);
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
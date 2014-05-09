namespace GitVersion
{
    using System;
    using System.Linq;

    public class MergedBranchesWithVersionFinder
    {
        Lazy<SemanticVersion> lastMergedBranchWithVersion;

        public MergedBranchesWithVersionFinder(GitVersionContext context)
        {
            lastMergedBranchWithVersion = new Lazy<SemanticVersion>(() => GetVersion(context));
        }

        public SemanticVersion GetVersion()
        {
            return lastMergedBranchWithVersion.Value;
        }

        SemanticVersion GetVersion(GitVersionContext context)
        {
            return context.CurrentBranch.Commits.Where(c =>
                {
                    string versionPart;
                    SemanticVersion semanticVersion;
                    return MergeMessageParser.TryParse(c, out versionPart) && SemanticVersion.TryParse(versionPart, out semanticVersion);
                })
                .Select(c =>
                {
                    string versionPart;
                    MergeMessageParser.TryParse(c, out versionPart);
                    return SemanticVersion.Parse(versionPart);
                })
                .Max();
        }
    }
}
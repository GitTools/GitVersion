namespace GitVersion
{
    using System;
    using System.Collections.Generic;
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
            var shortVersion = GetAllVersions(context)
                .OrderBy(x=>x.Major)
                .ThenBy(x=>x.Minor).ThenBy(x=>x.Patch)
                .LastOrDefault();
            if (shortVersion == null)
            {
                return null;
            }
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch
            };
        }

        static IEnumerable<ShortVersion> GetAllVersions(GitVersionContext context)
        {
            foreach (var commit in context.CurrentBranch.Commits)
            {
                ShortVersion version;
                if (MergeMessageParser.TryParse(commit, out version))
                {
                    yield return version;
                }
            }
        }
    }
}
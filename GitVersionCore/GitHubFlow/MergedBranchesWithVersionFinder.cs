namespace GitVersion
{
    using System.Collections.Generic;
    using System.Linq;

    public class MergedBranchesWithVersionFinder
    {
        GitVersionContext context;

        public MergedBranchesWithVersionFinder(GitVersionContext context)
        {
            this.context = context;
        }

        public bool TryGetVersion(out SemanticVersion semanticVersion)
        {
            var shortVersion = GetAllVersions(context)
                .OrderBy(x=>x.Major)
                .ThenBy(x=>x.Minor).ThenBy(x=>x.Patch)
                .LastOrDefault();
            if (shortVersion == null)
            {
                semanticVersion = null;
                return false;
            }
            semanticVersion =new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch
            };
            return true;
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
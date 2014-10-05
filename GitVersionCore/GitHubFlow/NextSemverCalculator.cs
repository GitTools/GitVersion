namespace GitVersion
{
    using System.Collections.Generic;
    using System.Linq;

    public class NextSemverCalculator
    {
        NextVersionTxtFileFinder nextVersionTxtFileFinder;
        LastTaggedReleaseFinder lastTaggedReleaseFinder;
        OtherBranchVersionFinder unknownBranchFinder;
        GitVersionContext context;
        MergedBranchesWithVersionFinder mergedBranchesWithVersionFinder;

        public NextSemverCalculator(
            NextVersionTxtFileFinder nextVersionTxtFileFinder,
            LastTaggedReleaseFinder lastTaggedReleaseFinder,
            GitVersionContext context)
        {
            this.nextVersionTxtFileFinder = nextVersionTxtFileFinder;
            this.lastTaggedReleaseFinder = lastTaggedReleaseFinder;
            mergedBranchesWithVersionFinder = new MergedBranchesWithVersionFinder(context);
            unknownBranchFinder = new OtherBranchVersionFinder();
            this.context = context;
        }

        public SemanticVersion NextVersion()
        {
            var versions = GetPossibleVersions().ToList();

            if (versions.Any())
            {
                return versions.Max();
            }
            return new SemanticVersion
            {
                Minor = 1
            };
        }

        public IEnumerable<SemanticVersion> GetPossibleVersions()
        {

            VersionTaggedCommit lastTaggedRelease;
            if (lastTaggedReleaseFinder.GetVersion(out lastTaggedRelease))
            {
                //If the exact commit is tagged then just return that commit
                if (context.CurrentCommit.Sha == lastTaggedRelease.Commit.Sha)
                {
                    yield return lastTaggedRelease.SemVer;
                    yield break;
                }
                yield return new SemanticVersion
                    {
                        Major = lastTaggedRelease.SemVer.Major,
                        Minor = lastTaggedRelease.SemVer.Minor,
                        Patch = lastTaggedRelease.SemVer.Patch + 1
                    };
            }

            SemanticVersion fileVersion;
            if (nextVersionTxtFileFinder.TryGetNextVersion(out fileVersion))
            {
                yield return fileVersion;
            }
            SemanticVersion tryGetVersion;
            if (mergedBranchesWithVersionFinder.TryGetVersion(out tryGetVersion))
            {
                yield return tryGetVersion;
            }

            SemanticVersion otherBranchVersion;
            if (unknownBranchFinder.FindVersion(context, out otherBranchVersion))
            {
                yield return otherBranchVersion;
            }

        }
    }
}
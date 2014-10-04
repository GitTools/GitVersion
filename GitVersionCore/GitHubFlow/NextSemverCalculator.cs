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
            var versionZero = new SemanticVersion();
            var lastRelease = lastTaggedReleaseFinder.GetVersion();
            var versions = new List<SemanticVersion>();
            SemanticVersion fileVersion;
            if(nextVersionTxtFileFinder.TryGetNextVersion(out fileVersion))
            {
                versions.Add(fileVersion);
            }
            SemanticVersion tryGetVersion;
            if (mergedBranchesWithVersionFinder.TryGetVersion(out tryGetVersion))
            {
                versions.Add(tryGetVersion);
            }

            var otherBranchVersion = unknownBranchFinder.FindVersion(context);
            versions.Add(otherBranchVersion);

            var maxCalculated = versions.Max();

            if (lastRelease.SemVer == versionZero && maxCalculated == versionZero)
            {
                return new SemanticVersion
                {
                    Minor = 1
                };
            }

            if (string.Equals(context.CurrentCommit.Sha, lastRelease.Commit.Sha))
            {
                return lastRelease.SemVer;
            }

            if (maxCalculated <= lastRelease.SemVer)
            {
                return new SemanticVersion
                {
                    Major = lastRelease.SemVer.Major,
                    Minor = lastRelease.SemVer.Minor,
                    Patch = lastRelease.SemVer.Patch + 1
                };
            }

            return maxCalculated;
        }
    }
}
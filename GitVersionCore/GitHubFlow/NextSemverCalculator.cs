namespace GitVersion
{
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
            var fileVersion = nextVersionTxtFileFinder.GetNextVersion();
            var mergedBranchVersion = mergedBranchesWithVersionFinder.GetVersion();
            var otherBranchVersion = unknownBranchFinder.FindVersion(context);
            if (otherBranchVersion != null && otherBranchVersion.PreReleaseTag != null && otherBranchVersion.PreReleaseTag.Name == "release")
                otherBranchVersion.PreReleaseTag.Name = "beta";

            var maxCalculated = new[] { fileVersion, otherBranchVersion, mergedBranchVersion }.Max();

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
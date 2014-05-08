namespace GitVersion
{
    public class NextSemverCalculator
    {
        NextVersionTxtFileFinder nextVersionTxtFileFinder;
        LastTaggedReleaseFinder lastTaggedReleaseFinder;
        MasterReleaseVersionFinder releaseVersionFinder;
        GitVersionContext context;

        public NextSemverCalculator(
            NextVersionTxtFileFinder nextVersionTxtFileFinder,
            LastTaggedReleaseFinder lastTaggedReleaseFinder,
            GitVersionContext context)
        {
            this.nextVersionTxtFileFinder = nextVersionTxtFileFinder;
            this.lastTaggedReleaseFinder = lastTaggedReleaseFinder;
            releaseVersionFinder = new MasterReleaseVersionFinder();
            this.context = context;
        }

        public SemanticVersion NextVersion()
        {
            var versionZero = new SemanticVersion();
            var lastRelease = lastTaggedReleaseFinder.GetVersion().SemVer;
            var fileVersion = nextVersionTxtFileFinder.GetNextVersion();
            var releaseVersion = context.CurrentBranch.IsRelease() ? releaseVersionFinder.FindVersion(context) : versionZero;
            var maxCalculated = fileVersion > releaseVersion ? fileVersion : releaseVersion;

            if (lastRelease == versionZero && maxCalculated == versionZero)
            {
                return new SemanticVersion
                {
                    Minor = 1
                };
            }

            if (maxCalculated <= lastRelease)
            {
                return new SemanticVersion
                {
                    Major = lastRelease.Major,
                    Minor = lastRelease.Minor,
                    Patch = lastRelease.Patch + 1
                };
            }

            return maxCalculated;
        }
    }
}
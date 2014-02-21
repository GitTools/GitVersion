namespace GitFlowVersion
{
    public class NextSemverCalculator
    {
        NextVersionTxtFileFinder nextVersionTxtFileFinder;
        LastTaggedReleaseFinder lastTaggedReleaseFinder;

        public NextSemverCalculator(
            NextVersionTxtFileFinder nextVersionTxtFileFinder,
            LastTaggedReleaseFinder lastTaggedReleaseFinder)
        {
            this.nextVersionTxtFileFinder = nextVersionTxtFileFinder;
            this.lastTaggedReleaseFinder = lastTaggedReleaseFinder;
        }

        public SemanticVersion NextVersion()
        {
            var lastRelease = lastTaggedReleaseFinder.GetVersion().SemVer;
            var fileVersion = nextVersionTxtFileFinder.GetNextVersion();
            if (fileVersion <= lastRelease)
            {
                return new SemanticVersion
                {
                    Major = lastRelease.Major,
                    Minor = lastRelease.Minor,
                    Patch = lastRelease.Patch + 1
                };
            }

            return fileVersion;
        }
    }
}
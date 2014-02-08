namespace GitFlowVersion
{
    public class NextSemverCalculator
    {
        private readonly NextVersionTxtFileFinder _nextVersionTxtFileFinder;
        private readonly LastTaggedReleaseFinder _lastTaggedReleaseFinder;

        public NextSemverCalculator(
            NextVersionTxtFileFinder nextVersionTxtFileFinder,
            LastTaggedReleaseFinder lastTaggedReleaseFinder)
        {
            _nextVersionTxtFileFinder = nextVersionTxtFileFinder;
            _lastTaggedReleaseFinder = lastTaggedReleaseFinder;
        }

        public SemanticVersion NextVersion()
        {
            var lastRelease = _lastTaggedReleaseFinder.GetVersion().SemVer;
            var fileVersion = _nextVersionTxtFileFinder.GetNextVersion();
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
namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using LibGit2Sharp;

    public class BaseVersion
    {
        public BaseVersion(bool shouldIncrement, bool shouldUpdateTag, SemanticVersion semanticVersion, Commit baseVersionSource)
        {
            ShouldIncrement = shouldIncrement;
            ShouldUpdateTag = shouldUpdateTag;
            SemanticVersion = semanticVersion;
            BaseVersionSource = baseVersionSource;
        }

        public bool ShouldIncrement { get; private set; }

        public bool ShouldUpdateTag { get; private set; }

        public SemanticVersion SemanticVersion { get; private set; }

        public Commit BaseVersionSource { get; private set; }
    }
}
namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class BaseVersion
    {
        public BaseVersion(bool shouldIncrement, SemanticVersion semanticVersion)
        {
            ShouldIncrement = shouldIncrement;
            SemanticVersion = semanticVersion;
        }

        public bool ShouldIncrement { get; private set; }

        public SemanticVersion SemanticVersion { get; private set; }
    }
}
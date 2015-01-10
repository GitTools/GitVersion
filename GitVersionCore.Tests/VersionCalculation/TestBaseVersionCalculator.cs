namespace GitVersionCore.Tests.VersionCalculation
{
    using GitVersion;
    using GitVersion.VersionCalculation;
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public class TestBaseVersionCalculator : IBaseVersionCalculator
    {
        readonly SemanticVersion semanticVersion;
        bool shouldIncrement;

        public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion)
        {
            this.semanticVersion = semanticVersion;
            this.shouldIncrement = shouldIncrement;
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            return new BaseVersion(shouldIncrement, semanticVersion);
        }
    }
}
namespace GitVersionCore.Tests.VersionCalculation
{
    using GitVersion;
    using GitVersion.VersionCalculation;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;

    public class TestBaseVersionCalculator : IBaseVersionCalculator
    {
        readonly SemanticVersion semanticVersion;
        bool shouldIncrement;
        bool shouldUpdateTag;
        Commit source;

        public TestBaseVersionCalculator(bool shouldIncrement, bool shouldUpdateTag, SemanticVersion semanticVersion, Commit source)
        {
            this.semanticVersion = semanticVersion;
            this.source = source;
            this.shouldUpdateTag = shouldUpdateTag;
            this.shouldIncrement = shouldIncrement;
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            return new BaseVersion(shouldIncrement, shouldUpdateTag, semanticVersion, source);
        }
    }
}
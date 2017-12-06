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
        BaseVersionSource source;

        public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, BaseVersionSource source)
        {
            this.semanticVersion = semanticVersion;
            this.source = source;
            this.shouldIncrement = shouldIncrement;
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            return new BaseVersion(context, shouldIncrement, semanticVersion, source, null);
        }
    }
}
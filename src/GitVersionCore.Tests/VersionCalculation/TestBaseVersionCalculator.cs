using GitVersion;
using GitVersion.SemanticVersioning;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestBaseVersionCalculator : IBaseVersionCalculator
    {
        private readonly SemanticVersion semanticVersion;
        private bool shouldIncrement;
        private Commit source;

        public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, Commit source)
        {
            this.semanticVersion = semanticVersion;
            this.source = source;
            this.shouldIncrement = shouldIncrement;
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            return new BaseVersion(context, "Test source", shouldIncrement, semanticVersion, source, null);
        }
    }
}
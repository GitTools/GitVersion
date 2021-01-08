using GitVersion;
using GitVersion.VersionCalculation;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestBaseVersionCalculator : IBaseVersionCalculator
    {
        private readonly SemanticVersion semanticVersion;
        private readonly bool shouldIncrement;
        private readonly ICommit source;

        public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, ICommit source)
        {
            this.semanticVersion = semanticVersion;
            this.source = source;
            this.shouldIncrement = shouldIncrement;
        }

        public BaseVersion GetBaseVersion()
        {
            return new BaseVersion("Test source", shouldIncrement, semanticVersion, source, null);
        }
    }
}

using GitVersion;
using GitVersion.Logging;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestBaseVersionCalculator : IBaseVersionCalculator
    {
        private readonly SemanticVersion semanticVersion;
        private readonly bool shouldIncrement;
        private readonly Commit source;

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

    public class TestBaseVersionStrategiesCalculator : BaseVersionCalculator
    {
        private static readonly IVersionStrategy[] VersionStrategies = new IVersionStrategy[]
        {
            new FallbackVersionStrategy(),
            new ConfigNextVersionVersionStrategy(),
            new TaggedCommitVersionStrategy(),
            new MergeMessageVersionStrategy(),
            new VersionInBranchNameVersionStrategy(),
            new TrackReleaseBranchesVersionStrategy()
        };
        public TestBaseVersionStrategiesCalculator(ILog log) : base(log, VersionStrategies)
        {
        }
    }
}

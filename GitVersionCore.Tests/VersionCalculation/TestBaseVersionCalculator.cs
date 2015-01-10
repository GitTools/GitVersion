namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
    using GitVersion;
    using GitVersion.VersionCalculation;
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public class TestBaseVersionCalculator : IBaseVersionCalculator
    {
        readonly SemanticVersion semanticVersion;
        bool shouldIncrement;
        DateTimeOffset? when;

        public TestBaseVersionCalculator(bool shouldIncrement, SemanticVersion semanticVersion, DateTimeOffset? when)
        {
            this.semanticVersion = semanticVersion;
            this.when = when;
            this.shouldIncrement = shouldIncrement;
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            return new BaseVersion(shouldIncrement, semanticVersion, when);
        }
    }
}
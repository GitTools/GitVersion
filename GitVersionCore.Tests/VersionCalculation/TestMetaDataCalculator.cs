namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
    using GitVersion;
    using GitVersion.VersionCalculation;

    public class TestMetaDataCalculator : IMetaDataCalculator
    {
        SemanticVersionBuildMetaData metaData;

        public TestMetaDataCalculator(SemanticVersionBuildMetaData metaData)
        {
            this.metaData = metaData;
        }

        public SemanticVersionBuildMetaData Create(DateTimeOffset? baseVersionWhenFrom, GitVersionContext context)
        {
            return metaData;
        }
    }
}
namespace GitVersionCore.Tests.VersionCalculation
{
    using GitVersion;
    using GitVersion.VersionCalculation;
    using LibGit2Sharp;

    public class TestMetaDataCalculator : IMetaDataCalculator
    {
        SemanticVersionBuildMetaData metaData;

        public TestMetaDataCalculator(SemanticVersionBuildMetaData metaData)
        {
            this.metaData = metaData;
        }

        public SemanticVersionBuildMetaData Create(int commitCount, GitVersionContext context)
        {
            return metaData;
        }
    }
}
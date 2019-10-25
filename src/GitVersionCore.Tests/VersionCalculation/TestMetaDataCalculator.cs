using GitVersion;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestMetaDataCalculator : IMetaDataCalculator
    {
        private readonly SemanticVersionBuildMetaData metaData;

        public TestMetaDataCalculator(SemanticVersionBuildMetaData metaData)
        {
            this.metaData = metaData;
        }

        public SemanticVersionBuildMetaData Create(Commit baseVersionSource, GitVersionContext context)
        {
            return metaData;
        }
    }
}
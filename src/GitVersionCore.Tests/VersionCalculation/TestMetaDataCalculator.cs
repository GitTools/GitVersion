using GitVersion;
using GitVersion.SemanticVersioning;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestMetaDataCalculator : IMetaDataCalculator
    {
        private SemanticVersionBuildMetaData metaData;

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
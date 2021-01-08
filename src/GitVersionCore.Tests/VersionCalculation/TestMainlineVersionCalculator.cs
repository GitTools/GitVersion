using GitVersion;
using GitVersion.VersionCalculation;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestMainlineVersionCalculator : IMainlineVersionCalculator
    {
        private readonly SemanticVersionBuildMetaData metaData;

        public TestMainlineVersionCalculator(SemanticVersionBuildMetaData metaData)
        {
            this.metaData = metaData;
        }

        public SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion)
        {
            throw new System.NotImplementedException();
        }

        public SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit baseVersionSource)
        {
            return metaData;
        }
    }
}

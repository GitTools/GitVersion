using GitVersion;
using GitVersion.VersionCalculation;
using LibGit2Sharp;

namespace GitVersionCore.Tests.VersionCalculation
{
    public class TestMainlineVersionCalculator : IMainlineVersionCalculator
    {
        private readonly SemanticVersionBuildMetaData metaData;

        public TestMainlineVersionCalculator(SemanticVersionBuildMetaData metaData)
        {
            this.metaData = metaData;
        }

        public SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context)
        {
            throw new System.NotImplementedException();
        }

        public SemanticVersionBuildMetaData CreateVersionBuildMetaData(Commit baseVersionSource, GitVersionContext context)
        {
            return metaData;
        }
    }
}

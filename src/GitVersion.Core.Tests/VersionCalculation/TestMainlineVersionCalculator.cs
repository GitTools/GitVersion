using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation;

public class TestMainlineVersionCalculator : IMainlineVersionCalculator
{
    private readonly SemanticVersionBuildMetaData metaData;

    public TestMainlineVersionCalculator(SemanticVersionBuildMetaData metaData) => this.metaData = metaData;

    public SemanticVersion FindMainlineModeVersion(NextVersion nextVersion) => throw new NotImplementedException();

    public SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource) => this.metaData;
}

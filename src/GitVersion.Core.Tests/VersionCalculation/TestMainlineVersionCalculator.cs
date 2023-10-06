using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.VersionCalculation;

public class TestMainlineVersionCalculator : IVersionModeCalculator
{
    private readonly SemanticVersionBuildMetaData metaData;

    public TestMainlineVersionCalculator(SemanticVersionBuildMetaData metaData) => this.metaData = metaData;

    public SemanticVersion Calculate(NextVersion nextVersion) => throw new NotImplementedException();

    public SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource) => this.metaData;
}

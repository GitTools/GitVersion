namespace GitVersion.VersionCalculation;

public interface IMainlineVersionCalculator
{
    SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion);
    SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource);
}

namespace GitVersion.VersionCalculation;

public interface IMainlineVersionCalculator
{
    SemanticVersion FindMainlineModeVersion(NextVersion nextVersion);

    SemanticVersionBuildMetaData CreateVersionBuildMetaData(ICommit? baseVersionSource);
}

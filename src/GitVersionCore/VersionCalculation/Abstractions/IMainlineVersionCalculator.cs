using LibGit2Sharp;

namespace GitVersion.VersionCalculation
{
    public interface IMainlineVersionCalculator
    {
        SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion);
        SemanticVersionBuildMetaData CreateVersionBuildMetaData(Commit baseVersionSource);
    }
}

using LibGit2Sharp;

namespace GitVersion.VersionCalculation
{
    public interface IMainlineVersionCalculator
    {
        SemanticVersion FindMainlineModeVersion(BaseVersion baseVersion, GitVersionContext context);
        SemanticVersionBuildMetaData CreateVersionBuildMetaData(Commit baseVersionSource, GitVersionContext context);
    }
}

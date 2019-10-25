using LibGit2Sharp;

namespace GitVersion.VersionCalculation
{
    public interface IMetaDataCalculator
    {
        SemanticVersionBuildMetaData Create(Commit baseVersionSource, GitVersionContext context);
    }
}

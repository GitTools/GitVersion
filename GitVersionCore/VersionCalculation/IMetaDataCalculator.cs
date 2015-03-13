namespace GitVersion.VersionCalculation
{
    using LibGit2Sharp;

    public interface IMetaDataCalculator
    {
        SemanticVersionBuildMetaData Create(Commit baseVersionSource, GitVersionContext context);
    }
}
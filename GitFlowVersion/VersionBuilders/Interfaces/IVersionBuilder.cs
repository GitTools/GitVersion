namespace GitFlowVersion.VersionBuilders
{
    public interface IVersionBuilder
    {
        string GenerateBuildVersion(VersionAndBranch versionAndBranch);
        string CreateVersionString(VersionAndBranch versionAndBranch);
    }
}

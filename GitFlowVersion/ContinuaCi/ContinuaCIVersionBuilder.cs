namespace GitFlowVersion.ContinuaCi
{
    using VersionBuilders;

    class ContinuaCiVersionBuilder : VersionBuilderBase
    {
        public override string GenerateBuildVersion(VersionAndBranch versionAndBranch)
        {
            var versionString = CreateVersionString(versionAndBranch);

            return string.Format("@@continua[setBuildVersion value='{0}']", versionString);
        }
    }
}

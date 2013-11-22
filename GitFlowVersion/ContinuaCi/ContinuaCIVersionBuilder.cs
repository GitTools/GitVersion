namespace GitFlowVersion.ContinuaCi
{
    using GitFlowVersion.VersionBuilders;

    class ContinuaCiVersionBuilder : VersionBuilderBase
    {
        public override string GenerateBuildVersion(VersionAndBranch versionAndBranch)
        {
            var versionString = CreateVersionString(versionAndBranch);

            return string.Format("##continuaci[buildNumber '{0}']", versionString);
        }
    }
}

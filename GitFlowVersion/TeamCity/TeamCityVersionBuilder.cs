namespace GitFlowVersion
{
    using global::GitFlowVersion.VersionBuilders;

    public class TeamCityVersionBuilder : VersionBuilderBase
    {
        public override string GenerateBuildVersion(VersionAndBranch versionAndBranch)
        {
            var versionString = CreateVersionString(versionAndBranch);

            return string.Format("##teamcity[buildNumber '{0}']", versionString);
        }
    }
}
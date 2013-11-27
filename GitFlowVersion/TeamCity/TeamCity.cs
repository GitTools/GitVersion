namespace GitFlowVersion
{
    using System;
    using GitFlowVersion.Integration;

    public class TeamCity : IntegrationBase
    {
        public override bool IsRunningInBuildAgent()
        {
            var isRunningInBuildAgent = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            if (isRunningInBuildAgent)
            {
                Logger.WriteInfo("Executing inside a TeamCityVersionBuilder build agent");
            }

            return isRunningInBuildAgent;
        }

        protected override string GenerateBuildParameter(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }
    }
}
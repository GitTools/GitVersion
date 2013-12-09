namespace GitFlowVersion
{
    using System;
    using GitFlowVersion;

    public class TeamCity : IBuildServer
    {
        public bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
        }

        public void PerformPreProcessingSteps(string gitDirectory)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new ErrorException("Failed to find .git directory on agent. Please make sure agent checkout mode is enabled for you VCS roots - http://confluence.jetbrains.com/display/TCD8/VCS+Checkout+Mode");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory);
        }

        public string GenerateSetParameterMessage(string name, string value)
        {
            return string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, value);
        }

        public string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return string.Format("##teamcity[buildNumber '{0}']", versionToUseForBuildNumber);
        }
    }
}

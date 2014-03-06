namespace GitVersion
{
    using System;

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

        public string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                // Note: see discussion at https://github.com/Particular/GitVersion/issues/90, keep GitFlowVersion for now
                string.Format("##teamcity[setParameter name='GitFlowVersion.{0}' value='{1}']", name, EscapeValue(value)),
                string.Format("##teamcity[setParameter name='GitVersion.{0}' value='{1}']", name, EscapeValue(value))
            };
        }

        public string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return string.Format("##teamcity[buildNumber '{0}']", EscapeValue(versionToUseForBuildNumber));
        }

        static string EscapeValue(string value)
        {
            // List of escape values from http://confluence.jetbrains.com/display/TCD8/Build+Script+Interaction+with+TeamCity

            value = value.Replace("|", "||");
            value = value.Replace("'", "|'");
            value = value.Replace("[", "|[");
            value = value.Replace("]", "|]");
            value = value.Replace("\r", "|r");
            value = value.Replace("\n", "|n");

            return value;
        }
    }
}

namespace GitVersion
{
    using System;

    public class MyGet : BuildServerBase
    {
        Authentication authentication;

        public MyGet(Authentication authentication)
        {
            this.authentication = authentication;
        }

        public override bool CanApplyToCurrentContext()
        {
            var buildRunner = Environment.GetEnvironmentVariable("BuildRunner");

            return !string.IsNullOrEmpty(buildRunner)
                && buildRunner.Equals("MyGet", StringComparison.InvariantCultureIgnoreCase);
        }

        public override void PerformPreProcessingSteps(string gitDirectory)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new WarningException("Failed to find .git directory on agent.");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory, authentication);
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("##teamcity[setParameter name='GitVersion.{0}' value='{1}']", name, EscapeValue(value))
            };
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return string.Format("##teamcity[buildNumber '{0}']", EscapeValue(versionToUseForBuildNumber));
        }

        static string EscapeValue(string value)
        {
            if (value == null)
            {
                return null;
            }
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

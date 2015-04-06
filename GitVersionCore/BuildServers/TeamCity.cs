namespace GitVersion
{
    using System;

    public class TeamCity : BuildServerBase
    {
        Authentication authentication;

        public TeamCity(Authentication authentication)
        {
            this.authentication = authentication;
        }

        public override bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
        }

        public override void PerformPreProcessingSteps(string gitDirectory, bool noFetch)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new WarningException("Failed to find .git directory on agent. Please make sure agent checkout mode is enabled for you VCS roots - http://confluence.jetbrains.com/display/TCD8/VCS+Checkout+Mode");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch);
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("##teamcity[setParameter name='GitVersion.{0}' value='{1}']", name, ServiceMessageEscapeHelper.EscapeValue(value)),
                string.Format("##teamcity[setParameter name='system.GitVersion.{0}' value='{1}']", name, ServiceMessageEscapeHelper.EscapeValue(value))
            };
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return string.Format("##teamcity[buildNumber '{0}']", ServiceMessageEscapeHelper.EscapeValue(versionToUseForBuildNumber));
        }
    }
}

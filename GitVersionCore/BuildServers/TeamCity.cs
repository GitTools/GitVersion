namespace GitVersion
{
    using System;

    public class TeamCity : BuildServerBase
    {
        public override bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
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

namespace GitVersion
{
    using System;
    using System.Collections.Generic;

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

        public override void PerformPreProcessingSteps(string gitDirectory, bool noFetch)
        {
            if (string.IsNullOrEmpty(gitDirectory))
            {
                throw new WarningException("Failed to find .git directory on agent.");
            }

            GitHelper.NormalizeGitDirectory(gitDirectory, authentication, noFetch);
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            var messages = new List<string>
            {
                string.Format("##myget[setParameter name='GitVersion.{0}' value='{1}']", name, ServiceMessageEscapeHelper.EscapeValue(value))
            };

            if (string.Equals(name, "LegacySemVerPadded", StringComparison.InvariantCultureIgnoreCase))
            {
                messages.Add(string.Format("##myget[buildNumber '{0}']", ServiceMessageEscapeHelper.EscapeValue(value)));
            }

            return messages.ToArray();
        }

        public override string GenerateSetVersionMessage(string versionToUseForBuildNumber)
        {
            return null;
        }
    }
}

namespace GitVersion
{
    using System;

    public class TeamCity : BuildServerBase
    {
		public const string EnvironmentVariableName = "TEAMCITY_VERSION";

		public override bool CanApplyToCurrentContext()
        {
			return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariableName));
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            var branchName = Environment.GetEnvironmentVariable("Git_Branch");

            if (string.IsNullOrEmpty(branchName))
            {
                if (!usingDynamicRepos)
                {
                    WriteBranchEnvVariableWarning();
                }

                return base.GetCurrentBranch(usingDynamicRepos);
            }

            return branchName;
        }

        static void WriteBranchEnvVariableWarning()
        {
            Logger.WriteWarning(@"TeamCity doesn't make the current branch available through environmental variables.

Depending on your authentication and transport setup of your git VCS root things may work. In that case, ignore this warning.

In your TeamCity build configuration, add a parameter called `env.Git_Branch` with value %teamcity.build.vcs.branch.<vcsid>%

See http://gitversion.readthedocs.org/en/latest/build-server-support/build-server/teamcity for more info");
        }

        public override bool PreventFetch()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Git_Branch"));
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                string.Format("##teamcity[setParameter name='GitVersion.{0}' value='{1}']", name, ServiceMessageEscapeHelper.EscapeValue(value)),
                string.Format("##teamcity[setParameter name='system.GitVersion.{0}' value='{1}']", name, ServiceMessageEscapeHelper.EscapeValue(value))
            };
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return string.Format("##teamcity[buildNumber '{0}']", ServiceMessageEscapeHelper.EscapeValue(variables.FullSemVer));
        }
    }
}

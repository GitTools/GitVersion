using GitVersion.Helpers;
using GitVersion.OutputVariables;
using GitVersion.Common;

namespace GitVersion.BuildServers
{
    public class TeamCity : BuildServerBase
    {
        public TeamCity(IEnvironment environment) : base(environment)
        {
        }

        public const string EnvironmentVariableName = "TEAMCITY_VERSION";

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

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
                $"##teamcity[setParameter name='GitVersion.{name}' value='{ServiceMessageEscapeHelper.EscapeValue(value)}']",
                $"##teamcity[setParameter name='system.GitVersion.{name}' value='{ServiceMessageEscapeHelper.EscapeValue(value)}']"
            };
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return $"##teamcity[buildNumber '{ServiceMessageEscapeHelper.EscapeValue(variables.FullSemVer)}']";
        }
    }
}

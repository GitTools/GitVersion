using System;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public class Drone : BuildAgentBase
    {
        public Drone(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "DRONE";
        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;
        public override bool CanApplyToCurrentContext()
        {
            return Environment.GetEnvironmentVariable(EnvironmentVariable)?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return variables.FullSemVer;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                $"GitVersion_{name}={value}"
            };
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            // In Drone DRONE_BRANCH variable is equal to destination branch in case of pull request
            // https://discourse.drone.io/t/getting-the-branch-a-pull-request-is-created-from/670
            // Unfortunately, DRONE_REFSPEC isn't populated, however CI_COMMIT_REFSPEC can be used instead of.
            var pullRequestNumber = Environment.GetEnvironmentVariable("DRONE_PULL_REQUEST");
            if (!string.IsNullOrWhiteSpace(pullRequestNumber))
            {
                // DRONE_SOURCE_BRANCH is available in Drone 1.x.x version
                var sourceBranch = Environment.GetEnvironmentVariable("DRONE_SOURCE_BRANCH");
                if (!string.IsNullOrWhiteSpace(sourceBranch))
                    return sourceBranch;

                // In drone lower than 1.x.x source branch can be parsed from CI_COMMIT_REFSPEC
                // CI_COMMIT_REFSPEC - {sourceBranch}:{destinationBranch}
                // https://github.com/drone/drone/issues/2222
                var ciCommitRefSpec = Environment.GetEnvironmentVariable("CI_COMMIT_REFSPEC");
                if (!string.IsNullOrWhiteSpace(ciCommitRefSpec))
                {
                    var colonIndex = ciCommitRefSpec.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        return ciCommitRefSpec.Substring(0, colonIndex);
                    }
                }
            }

            return Environment.GetEnvironmentVariable("DRONE_BRANCH");
        }

        public override bool PreventFetch() => false;
    }
}

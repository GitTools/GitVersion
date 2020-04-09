using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public class AzurePipelines : BuildAgentBase
    {
        public AzurePipelines(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "TF_BUILD";

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                $"##vso[task.setvariable variable=GitVersion.{name}]{value}",
                $"##vso[task.setvariable variable=GitVersion.{name};isOutput=true]{value}"
            };
        }

        public override string GetCurrentBranch(bool usingDynamicRepos)
        {
            return Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCH");
        }

        public override bool PreventFetch() => true;

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            // For AzurePipelines, we'll get the Build Number and insert GitVersion variables where
            // specified
            var buildNumberEnv = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER");
            if (string.IsNullOrWhiteSpace(buildNumberEnv))
                return variables.FullSemVer;

            var newBuildNumber = variables.Aggregate(buildNumberEnv, ReplaceVariables);

            // If no variable substitution has happened, use FullSemVer
            if (buildNumberEnv == newBuildNumber)
            {
                var buildNumber = variables.FullSemVer.EndsWith("+0")
                                ? variables.FullSemVer.Substring(0, variables.FullSemVer.Length - 2)
                                : variables.FullSemVer;

                return $"##vso[build.updatebuildnumber]{buildNumber}";
            }

            return $"##vso[build.updatebuildnumber]{newBuildNumber}";
        }

        private static string ReplaceVariables(string buildNumberEnv, KeyValuePair<string, string> variable)
        {
            var pattern = $@"\$\(GITVERSION[_\.]{variable.Key}\)";
            var replacement = variable.Value ?? string.Empty;
            return buildNumberEnv.RegexReplace(pattern, replacement, RegexOptions.IgnoreCase);
        }
    }
}

using System;
using System.IO;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public class GitLabCi : BuildAgentBase
    {
        public const string EnvironmentVariableName = "GITLAB_CI";
        private string file;

        public GitLabCi(IEnvironment environment, ILog log) : base(environment, log)
        {
            WithPropertyFile("gitversion.properties");
        }

        public void WithPropertyFile(string propertiesFileName)
        {
            file = propertiesFileName;
        }

        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;


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
            return Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME");
        }

        public override bool PreventFetch() => true;

        public override void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            base.WriteIntegration(writer, variables);
            writer($"Outputting variables to '{file}' ... ");
            WriteVariablesFile(variables);
        }

        private void WriteVariablesFile(VersionVariables variables)
        {
            File.WriteAllLines(file, GenerateBuildLogOutput(variables));
        }
    }
}

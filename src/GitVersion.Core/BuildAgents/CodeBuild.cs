using System;
using System.IO;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public sealed class CodeBuild : BuildAgentBase
    {
        private string file;
        public const string WebHookEnvironmentVariableName = "CODEBUILD_WEBHOOK_HEAD_REF";
        public const string SourceVersionEnvironmentVariableName = "CODEBUILD_SOURCE_VERSION";

        public CodeBuild(IEnvironment environment, ILog log) : base(environment, log)
        {
            WithPropertyFile("gitversion.properties");
        }

        public void WithPropertyFile(string propertiesFileName)
        {
            file = propertiesFileName;
        }

        protected override string EnvironmentVariable => throw new NotSupportedException($"Accessing {nameof(EnvironmentVariable)} is not supported as {nameof(CodeBuild)} supports two environment variables for branch names.");

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

            var currentBranch = Environment.GetEnvironmentVariable(WebHookEnvironmentVariableName);

            if (string.IsNullOrEmpty(currentBranch))
            {
                return Environment.GetEnvironmentVariable(SourceVersionEnvironmentVariableName);
            }

            return currentBranch;
        }

        public override void WriteIntegration(Action<string> writer, VersionVariables variables, bool updateBuildNumber = true)
        {
            base.WriteIntegration(writer, variables);
            writer($"Outputting variables to '{file}' ... ");
            File.WriteAllLines(file, GenerateBuildLogOutput(variables));
        }

        public override bool PreventFetch() => true;

        public override bool CanApplyToCurrentContext()
            => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(WebHookEnvironmentVariableName)) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(SourceVersionEnvironmentVariableName));
    }
}

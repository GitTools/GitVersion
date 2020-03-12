using System;
using System.IO;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion.Logging;

namespace GitVersion.BuildServers
{
    public sealed class CodeBuild : BuildServerBase
    {
        private string file;
        public const string EnvironmentVariableName = "CODEBUILD_WEBHOOK_HEAD_REF";

        public CodeBuild(IEnvironment environment, ILog log) : base(environment, log)
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
            return Environment.GetEnvironmentVariable(EnvironmentVariableName);
        }

        public override void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            base.WriteIntegration(writer, variables);
            writer($"Outputting variables to '{file}' ... ");
            File.WriteAllLines(file, BuildOutputFormatter.GenerateBuildLogOutput(this, variables));
        }

        public override bool PreventFetch() => true;
    }
}

using System;
using System.IO;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion.Logging;

namespace GitVersion.BuildServers
{
    public sealed class CodeBuild : BuildServerBase
    {
        public const string HeadRefEnvironmentName = "CODEBUILD_WEBHOOK_HEAD_REF";
        private readonly string propertiesFileName;

        public CodeBuild(IEnvironment environment, ILog log, string propertiesFileName = "gitversion.properties") : base(environment, log)
        {
            this.propertiesFileName = propertiesFileName;
        }

        protected override string EnvironmentVariable { get; } = HeadRefEnvironmentName;

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
            return Environment.GetEnvironmentVariable(HeadRefEnvironmentName);
        }

        public override void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            base.WriteIntegration(writer, variables);
            writer($"Outputting variables to '{propertiesFileName}' ... ");
            File.WriteAllLines(propertiesFileName, BuildOutputFormatter.GenerateBuildLogOutput(this, variables));
        }

        public override bool PreventFetch() => true;
    }
}

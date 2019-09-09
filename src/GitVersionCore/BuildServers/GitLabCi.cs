using System;
using System.IO;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion.Common;

namespace GitVersion.BuildServers
{
    public class GitLabCi : BuildServerBase
    {
        public const string EnvironmentVariableName = "GITLAB_CI";
        string _file;
        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;

        public GitLabCi()
            : this("gitversion.properties")
        {
        }

        public GitLabCi(string propertiesFileName)
        {
            _file = propertiesFileName;
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
            return Environment.GetEnvironmentVariable("CI_COMMIT_REF_NAME");
        }

        public override bool PreventFetch() => true;

        public override void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            base.WriteIntegration(writer, variables);
            writer($"Outputting variables to '{_file}' ... ");
            WriteVariablesFile(variables);
        }

        void WriteVariablesFile(VersionVariables variables)
        {
            File.WriteAllLines(_file, BuildOutputFormatter.GenerateBuildLogOutput(this, variables));
        }
    }
}

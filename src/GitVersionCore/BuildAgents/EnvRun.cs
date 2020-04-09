using System.IO;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.BuildAgents
{
    public class EnvRun : BuildAgentBase
    {
        public EnvRun(IEnvironment environment, ILog log) : base(environment, log)
        {
        }

        public const string EnvironmentVariableName = "ENVRUN_DATABASE";
        protected override string EnvironmentVariable { get; } = EnvironmentVariableName;
        public override bool CanApplyToCurrentContext()
        {
            var envRunDatabasePath = Environment.GetEnvironmentVariable(EnvironmentVariableName);
            if (!string.IsNullOrEmpty(envRunDatabasePath))
            {
                if (!File.Exists(envRunDatabasePath))
                {
                    Log.Error($"The database file of EnvRun.exe was not found at {envRunDatabasePath}.");
                    return false;
                }

                return true;
            }

            return false;
        }

        public override string GenerateSetVersionMessage(VersionVariables variables)
        {
            return variables.FullSemVer;
        }

        public override string[] GenerateSetParameterMessage(string name, string value)
        {
            return new[]
            {
                $"@@envrun[set name='GitVersion_{name}' value='{value}']"
            };
        }
        public override bool PreventFetch() => true;
    }
}
